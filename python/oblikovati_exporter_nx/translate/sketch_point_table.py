# SPDX-License-Identifier: GPL-2.0-only
"""Allocates one distinct recipe point per curve endpoint/center.

This mirrors how the Oblikovati engine itself serializes sketches: each curve keeps its
own points and coincidence is expressed by ``coincident`` CONSTRAINTS, not by sharing
ids (confirmed by round-tripping an engine-authored rectangle — merging endpoints into
shared ids instead yields zero detected profiles). Coordinates convert from the IR's
millimetres to the recipe's centimetre database unit.
"""
from __future__ import annotations

from typing import Dict, List, Tuple

from ..model.sketch import NxCurve, NxCurveKind, NxCurvePointRole, NxPointRef, NxSketch
from ..recipe.sketch import PointData
from .id_allocator import IdAllocator
from .units import MM_TO_CM

_Slot = Tuple[int, NxCurvePointRole]


class SketchPointTable:
    def __init__(self) -> None:
        self._slot_to_point_id: Dict[_Slot, int] = {}
        self._points: List[PointData] = []

    @property
    def points(self) -> List[PointData]:
        return self._points

    def point_id(self, reference: NxPointRef) -> int:
        return self._slot_to_point_id[(reference.curve_id, reference.role)]

    def build(self, sketch: NxSketch, ids: IdAllocator) -> None:
        for curve in sketch.curves:
            for role in _roles_of(curve.kind):
                point_id = ids.next()
                self._slot_to_point_id[(curve.id, role)] = point_id
                xy = _coord_of(curve, role)
                self._points.append(PointData(id=point_id, x=xy[0] * MM_TO_CM, y=xy[1] * MM_TO_CM))


def _coord_of(curve: NxCurve, role: NxCurvePointRole) -> List[float]:
    if role == NxCurvePointRole.START:
        return curve.start
    if role == NxCurvePointRole.END:
        return curve.end
    return curve.center


def _roles_of(kind: NxCurveKind) -> List[NxCurvePointRole]:
    if kind == NxCurveKind.LINE:
        return [NxCurvePointRole.START, NxCurvePointRole.END]
    if kind == NxCurveKind.CIRCLE:
        return [NxCurvePointRole.CENTER]
    return [NxCurvePointRole.CENTER, NxCurvePointRole.START, NxCurvePointRole.END]
