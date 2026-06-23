# SPDX-License-Identifier: GPL-2.0-only
"""Reads a part's sketches into the IR.

The sketch plane is fitted from the curves' 3D points (avoiding the uncertain NX
sketch-plane API); points project into that frame as 2D coordinates. Lines and full
circles are extracted; coincidence is inferred from endpoints that meet, so profiles
close in Oblikovati (which is what makes a profile extrudable). Partial arcs, splines,
and NX's explicit constraints/dimensions are not yet read — flagged for live-NX
completion (geometry is positioned correctly; the missing dimensions are the parametric
refinement). UNVERIFIED: needs a real NX session.

Identity maps key on the stable NXOpen ``.Tag`` rather than Python object identity,
since NXOpen may hand back a fresh wrapper per call for the same underlying object.
"""
from __future__ import annotations

import math
from typing import Dict, List, Optional

import NXOpen

from ..model import sketch_plane_math as spm
from ..model.sketch import (
    NxConstraintKind,
    NxCurve,
    NxCurveKind,
    NxCurvePointRole,
    NxPointRef,
    NxSketch,
    NxSketchConstraint,
)

_COINCIDENCE_TOL = 1e-4  # mm, in sketch 2D


def extract(part, document, curve_tag_to_sketch: Dict[int, int]) -> None:
    for sketch in part.Sketches:
        geometry = list(sketch.GetAllGeometry())
        extracted = _extract_one(sketch, geometry)
        if extracted is None:
            continue
        index = len(document.sketches)
        document.sketches.append(extracted)
        # Map this sketch's curves so a feature's section resolves to its sketch index.
        for obj in geometry:
            curve_tag_to_sketch[obj.Tag] = index


def _extract_one(sketch, geometry) -> Optional[NxSketch]:
    frame = spm.fit(_collect_points(geometry))
    result = NxSketch(
        name=sketch.Name, origin=frame.origin, xaxis=frame.xaxis, yaxis=frame.yaxis
    )

    next_id = 1
    for obj in geometry:
        if isinstance(obj, NXOpen.Line):
            result.curves.append(_line_curve(next_id, obj, frame))
            next_id += 1
        elif isinstance(obj, NXOpen.Arc) and _is_full_circle(obj):
            result.curves.append(_circle_curve(next_id, obj, frame))
            next_id += 1

    _infer_coincidences(result)
    return None if not result.curves else result


def _collect_points(geometry) -> List[List[float]]:
    points: List[List[float]] = []
    for obj in geometry:
        if isinstance(obj, NXOpen.Line):
            points.append(_p(obj.StartPoint))
            points.append(_p(obj.EndPoint))
        elif isinstance(obj, NXOpen.Arc):
            points.append(_p(obj.CenterPoint))
    return points


def _line_curve(curve_id: int, line, frame: spm.SketchPlaneFrame) -> NxCurve:
    return NxCurve(
        id=curve_id,
        kind=NxCurveKind.LINE,
        start=frame.to_2d(_p(line.StartPoint)),
        end=frame.to_2d(_p(line.EndPoint)),
    )


def _circle_curve(curve_id: int, arc, frame: spm.SketchPlaneFrame) -> NxCurve:
    return NxCurve(
        id=curve_id,
        kind=NxCurveKind.CIRCLE,
        center=frame.to_2d(_p(arc.CenterPoint)),
        radius=arc.Radius,
    )


# Emit a coincident constraint for each pair of line endpoints that meet, so the profile
# closes (mirrors how the engine records coincidence between distinct points).
def _infer_coincidences(sketch: NxSketch) -> None:
    slots = []
    for curve in sketch.curves:
        if curve.kind != NxCurveKind.LINE:
            continue
        slots.append((NxPointRef(curve.id, NxCurvePointRole.START), curve.start))
        slots.append((NxPointRef(curve.id, NxCurvePointRole.END), curve.end))

    for i in range(len(slots)):
        for j in range(i + 1, len(slots)):
            if slots[i][0].curve_id == slots[j][0].curve_id:
                continue
            if _distance_2d(slots[i][1], slots[j][1]) <= _COINCIDENCE_TOL:
                constraint = NxSketchConstraint(kind=NxConstraintKind.COINCIDENT)
                constraint.points.append(slots[i][0])
                constraint.points.append(slots[j][0])
                sketch.constraints.append(constraint)


def _is_full_circle(arc) -> bool:
    return abs((arc.EndAngle - arc.StartAngle) - 2 * math.pi) < 1e-6


def _p(point) -> List[float]:
    return [point.X, point.Y, point.Z]


def _distance_2d(a, b) -> float:
    dx = a[0] - b[0]
    dy = a[1] - b[1]
    return math.sqrt(dx * dx + dy * dy)
