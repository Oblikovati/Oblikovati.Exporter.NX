# SPDX-License-Identifier: GPL-2.0-only
"""Reads a sketch's real geometric constraints and dimensions (the NXOpen-aware half).

NX exposes constraints via ``sketch.GetAllConstraintsOfType(ConstraintClass, ConstraintType)``;
each ``SketchGeometricConstraint.GetGeometry()`` returns ``Sketch.ConstraintGeometry`` items
that carry the constrained object (``.Geometry``) and which defining point of it
(``.PointType``). We query per type so the kind is known, then split each constraint's
geometry into point operands and curve operands and emit the IR constraint shape the
translator expects. Dimensions come from the Dimension class with their driving expression.

When no geometric constraints are read (older parts, or an API mismatch), the caller falls
back to inferring coincidence from meeting endpoints, so profiles still close.

UNVERIFIED — needs a real NX session; the NX ConstraintType/ConstraintPointType member
names and the dimension expression/geometry accessors are best-effort vs the documented API
and must be confirmed live. Anything that can't be mapped is reported, never emitted wrong.
"""
from __future__ import annotations

from typing import Dict, List, Optional, Tuple

import NXOpen

from ..model.sketch import (
    NxConstraintKind,
    NxCurvePointRole,
    NxDimensionKind,
    NxPointRef,
    NxSketch,
    NxSketchConstraint,
    NxSketchDimension,
)

# NX geometric ConstraintType member name -> our kind. Looked up via getattr so a name
# absent in a given NX version is simply skipped rather than crashing.
_GEOMETRIC_TYPES = {
    "Coincident": NxConstraintKind.COINCIDENT,
    "Horizontal": NxConstraintKind.HORIZONTAL,
    "Vertical": NxConstraintKind.VERTICAL,
    "Parallel": NxConstraintKind.PARALLEL,
    "Perpendicular": NxConstraintKind.PERPENDICULAR,
    "Collinear": NxConstraintKind.COLLINEAR,
    "EqualLength": NxConstraintKind.EQUAL_LENGTH,
    "Concentric": NxConstraintKind.CONCENTRIC,
    "EqualRadius": NxConstraintKind.EQUAL_RADIUS,
    "Tangent": NxConstraintKind.TANGENT,
    "PointOnCurve": NxConstraintKind.POINT_ON_LINE,
    "MidPoint": NxConstraintKind.MIDPOINT,
    "Fixed": NxConstraintKind.FIX,
    "Mirror": NxConstraintKind.SYMMETRY,
}

# NX ConstraintPointType member name -> our curve point role.
_POINT_ROLES = {
    "StartVertex": NxCurvePointRole.START,
    "Start": NxCurvePointRole.START,
    "EndVertex": NxCurvePointRole.END,
    "End": NxCurvePointRole.END,
    "CenterPoint": NxCurvePointRole.CENTER,
    "Center": NxCurvePointRole.CENTER,
}


def read_constraints(
    sketch, result: NxSketch, tag_to_curve_id: Dict[int, int], report
) -> int:
    """Appends real geometric constraints to ``result``. Returns how many were read."""
    count = 0
    for type_name, kind in _GEOMETRIC_TYPES.items():
        nx_type = _constraint_type(type_name)
        if nx_type is None:
            continue
        for raw in _of_type(sketch, NXOpen.Sketch.ConstraintClass.Geometric, nx_type):
            constraint = _build(kind, raw, tag_to_curve_id, report)
            if constraint is not None:
                result.constraints.append(constraint)
                count += 1
    return count


def read_dimensions(sketch, result: NxSketch, tag_to_curve_id: Dict[int, int], report) -> None:
    """Appends real dimensional constraints (type + driving expression + geometry)."""
    no_con = getattr(NXOpen.Sketch.ConstraintType, "NoCon", None)
    if no_con is None:
        return
    for raw in _of_type(sketch, NXOpen.Sketch.ConstraintClass.Dimension, no_con):
        dimension = _build_dimension(raw, tag_to_curve_id, report)
        if dimension is not None:
            result.dimensions.append(dimension)


def _build(
    kind: NxConstraintKind, raw, tag_to_curve_id: Dict[int, int], report
) -> Optional[NxSketchConstraint]:
    points, curves = _split_geometry(raw, tag_to_curve_id)
    constraint = NxSketchConstraint(kind=kind)

    if kind == NxConstraintKind.COINCIDENT:
        if len(points) < 2:
            return _skip(report, kind, "expected 2 point operands")
        constraint.points.extend(points[:2])
    elif kind in (NxConstraintKind.HORIZONTAL, NxConstraintKind.VERTICAL):
        if not curves:
            return _skip(report, kind, "expected a line operand")
        constraint.curves.append(curves[0])
    elif kind in (
        NxConstraintKind.PARALLEL, NxConstraintKind.PERPENDICULAR, NxConstraintKind.COLLINEAR,
        NxConstraintKind.EQUAL_LENGTH, NxConstraintKind.CONCENTRIC, NxConstraintKind.EQUAL_RADIUS,
        NxConstraintKind.TANGENT,
    ):
        if len(curves) < 2:
            return _skip(report, kind, "expected 2 curve operands")
        constraint.curves.extend(curves[:2])
    elif kind in (NxConstraintKind.POINT_ON_LINE, NxConstraintKind.MIDPOINT):
        if not points or not curves:
            return _skip(report, kind, "expected a point and a curve")
        constraint.points.append(points[0])
        constraint.curves.append(curves[0])
    elif kind == NxConstraintKind.FIX:
        if not points:
            return _skip(report, kind, "expected a fixed point")
        constraint.points.append(points[0])
    elif kind == NxConstraintKind.SYMMETRY:
        if len(points) < 2 or not curves:
            return _skip(report, kind, "expected 2 points and an axis")
        constraint.points.extend(points[:2])
        constraint.curves.append(curves[0])
    else:
        return _skip(report, kind, "unhandled kind")
    return constraint


def _build_dimension(raw, tag_to_curve_id: Dict[int, int], report) -> Optional[NxSketchDimension]:
    kind = _dimension_kind(raw)
    if kind is None:
        report.warn("sketch dimension of an unrecognised type was skipped")
        return None
    expression = _dimension_expression(raw)
    if expression is None:
        report.warn("sketch dimension without a readable expression was skipped")
        return None

    dimension = NxSketchDimension(kind=kind, expression=expression, driven=_is_driven(raw))
    points, curves = _split_geometry(raw, tag_to_curve_id)
    if kind == NxDimensionKind.DISTANCE:
        if len(points) < 2:
            report.warn("distance dimension without two point operands was skipped")
            return None
        dimension.points.extend(points[:2])
    else:
        if not curves:
            report.warn(f"{kind.value} dimension without a curve operand was skipped")
            return None
        dimension.curves.append(curves[0])
    return dimension


# Splits a constraint's geometry into (point refs, curve ids) using each item's PointType.
def _split_geometry(
    raw, tag_to_curve_id: Dict[int, int]
) -> Tuple[List[NxPointRef], List[int]]:
    points: List[NxPointRef] = []
    curves: List[int] = []
    for item in raw.GetGeometry():
        geometry = item.Geometry
        if geometry is None:
            continue
        curve_id = tag_to_curve_id.get(geometry.Tag)
        if curve_id is None:
            continue
        role = _point_role(item.PointType)
        if role is None:
            curves.append(curve_id)
        else:
            points.append(NxPointRef(curve_id, role))
    return points, curves


def _of_type(sketch, constraint_class, constraint_type):
    found = sketch.GetAllConstraintsOfType(constraint_class, constraint_type)
    return list(found) if found is not None else []


def _constraint_type(name: str):
    return getattr(NXOpen.Sketch.ConstraintType, name, None)


def _point_role(point_type) -> Optional[NxCurvePointRole]:
    return _POINT_ROLES.get(_enum_name(point_type))


def _dimension_kind(raw) -> Optional[NxDimensionKind]:
    name = _enum_name(getattr(raw, "ConstraintType", None))
    mapping = {
        "ParallelDim": NxDimensionKind.DISTANCE,
        "PerpendicularDim": NxDimensionKind.DISTANCE,
        "HorizontalDim": NxDimensionKind.DISTANCE,
        "VerticalDim": NxDimensionKind.DISTANCE,
        "Radius": NxDimensionKind.RADIUS,
        "Diameter": NxDimensionKind.DIAMETER,
        "Angular": NxDimensionKind.ANGLE,
    }
    return mapping.get(name)


def _dimension_expression(raw) -> Optional[str]:
    # The driving expression carries the formula (a parameter name or a literal "40").
    expression = getattr(raw, "AssociatedExpression", None)
    if expression is not None and getattr(expression, "RightHandSide", None):
        return expression.RightHandSide
    return None


def _is_driven(raw) -> bool:
    state = _enum_name(getattr(raw, "DimensionState", None))
    return state in ("Reference", "Driven", "Automatic")


def _enum_name(value) -> str:
    if value is None:
        return ""
    return getattr(value, "name", str(value)).split(".")[-1]


def _skip(report, kind: NxConstraintKind, why: str) -> None:
    report.warn(f"sketch constraint '{kind.value}' skipped: {why}")
    return None
