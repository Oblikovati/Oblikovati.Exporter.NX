# SPDX-License-Identifier: GPL-2.0-only
"""Translates one NX sketch into an Oblikovati SketchData.

Shared points (via SketchPointTable), curve entities, geometric constraints, and
parameter-linked dimensions. Lengths convert from the IR's millimetres to the recipe's
centimetre database unit.
"""
from __future__ import annotations

from typing import Dict, List, Optional

from ..model.sketch import (
    NxConstraintKind,
    NxCurve,
    NxCurveKind,
    NxCurvePointRole,
    NxDimensionKind,
    NxPointRef,
    NxSketch,
    NxSketchConstraint,
    NxSketchDimension,
)
from ..recipe.sketch import (
    ConstraintData,
    DimensionData,
    EntityData,
    PlaneData,
    SketchData,
)
from .id_allocator import IdAllocator
from .report import ExportReport
from .sketch_point_table import SketchPointTable
from .units import MM_TO_CM, scale_point

# Constraint kinds whose operands are two-or-more curve ids.
_CURVE_CONSTRAINTS = frozenset(
    {
        NxConstraintKind.PARALLEL,
        NxConstraintKind.PERPENDICULAR,
        NxConstraintKind.COLLINEAR,
        NxConstraintKind.EQUAL_LENGTH,
        NxConstraintKind.CONCENTRIC,
        NxConstraintKind.EQUAL_RADIUS,
        NxConstraintKind.TANGENT,
    }
)


class SketchTranslator:
    def __init__(self, ids: IdAllocator, report: ExportReport) -> None:
        self._ids = ids
        self._report = report

    def translate(self, sketch: NxSketch, sketch_id: int) -> SketchData:
        points = SketchPointTable()
        points.build(sketch, self._ids)

        data = SketchData(
            id=sketch_id,
            name=sketch.name or None,
            plane=_translate_plane(sketch),
            points=list(points.points),
        )
        entity_ids = self._add_entities(sketch, points, data)
        self._add_constraints(sketch, points, entity_ids, data)
        self._add_dimensions(sketch, points, entity_ids, data)
        return data

    def _add_entities(
        self, sketch: NxSketch, points: SketchPointTable, data: SketchData
    ) -> Dict[int, int]:
        entity_ids: Dict[int, int] = {}
        for curve in sketch.curves:
            entity_id = self._ids.next()
            entity_ids[curve.id] = entity_id
            data.entities.append(_build_entity(entity_id, curve, points))
        return entity_ids

    def _add_constraints(
        self,
        sketch: NxSketch,
        points: SketchPointTable,
        entity_ids: Dict[int, int],
        data: SketchData,
    ) -> None:
        for constraint in sketch.constraints:
            row = self._build_constraint(constraint, points, entity_ids)
            if row is not None:
                data.constraints.append(row)

    def _build_constraint(
        self,
        constraint: NxSketchConstraint,
        points: SketchPointTable,
        entity_ids: Dict[int, int],
    ) -> Optional[ConstraintData]:
        kind = constraint.kind
        row = ConstraintData(kind=kind.value)
        if kind == NxConstraintKind.COINCIDENT:
            row.points.append(points.point_id(constraint.points[0]))
            row.points.append(points.point_id(constraint.points[1]))
            return row
        if kind in (NxConstraintKind.HORIZONTAL, NxConstraintKind.VERTICAL):
            # NX applies these to a line; Oblikovati constrains its two endpoints.
            line = constraint.curves[0]
            row.points.append(points.point_id(NxPointRef(line, NxCurvePointRole.START)))
            row.points.append(points.point_id(NxPointRef(line, NxCurvePointRole.END)))
            return row
        if kind in _CURVE_CONSTRAINTS:
            for curve_id in constraint.curves:
                row.curves.append(entity_ids[curve_id])
            return row
        if kind in (NxConstraintKind.POINT_ON_LINE, NxConstraintKind.MIDPOINT):
            row.points.append(points.point_id(constraint.points[0]))
            row.curves.append(entity_ids[constraint.curves[0]])
            return row
        if kind == NxConstraintKind.FIX:
            row.points.append(points.point_id(constraint.points[0]))
            return row
        self._report.unsupported("sketch-constraint", kind.name)
        return None

    def _add_dimensions(
        self,
        sketch: NxSketch,
        points: SketchPointTable,
        entity_ids: Dict[int, int],
        data: SketchData,
    ) -> None:
        for dimension in sketch.dimensions:
            data.dimensions.append(_build_dimension(dimension, points, entity_ids))


def _translate_plane(sketch: NxSketch) -> PlaneData:
    return PlaneData(
        origin=scale_point(sketch.origin),
        xaxis=list(sketch.xaxis),
        yaxis=list(sketch.yaxis),
    )


def _build_entity(entity_id: int, curve: NxCurve, points: SketchPointTable) -> EntityData:
    entity = EntityData(
        id=entity_id,
        kind=curve.kind.value,
        construction=True if curve.construction else None,
    )
    if curve.kind == NxCurveKind.LINE:
        entity.points.append(points.point_id(NxPointRef(curve.id, NxCurvePointRole.START)))
        entity.points.append(points.point_id(NxPointRef(curve.id, NxCurvePointRole.END)))
        entity.centerline = True if curve.centerline else None
    elif curve.kind == NxCurveKind.CIRCLE:
        entity.points.append(points.point_id(NxPointRef(curve.id, NxCurvePointRole.CENTER)))
        entity.radius = curve.radius * MM_TO_CM
    else:  # arc
        entity.points.append(points.point_id(NxPointRef(curve.id, NxCurvePointRole.CENTER)))
        entity.points.append(points.point_id(NxPointRef(curve.id, NxCurvePointRole.START)))
        entity.points.append(points.point_id(NxPointRef(curve.id, NxCurvePointRole.END)))
        entity.ccw = True if curve.ccw else None
    return entity


def _build_dimension(
    dimension: NxSketchDimension,
    points: SketchPointTable,
    entity_ids: Dict[int, int],
) -> DimensionData:
    row = DimensionData(
        kind=dimension.kind.value,
        expression=dimension.expression,
        driven=True if dimension.driven else None,
    )
    if dimension.kind == NxDimensionKind.DISTANCE:
        for point_ref in dimension.points:
            row.points.append(points.point_id(point_ref))
    else:
        for curve_id in dimension.curves:
            row.curves.append(entity_ids[curve_id])
    return row
