# SPDX-License-Identifier: GPL-2.0-only
"""Reads a part's sketches into the IR: geometry, real constraints, real dimensions.

Geometry (lines, circles, arcs, ellipses, splines) and the fitted plane come from
``sketch_geometry``; the real NX geometric constraints and dimensional constraints come
from ``sketch_constraint_reader``. When NX returns no geometric constraints (an older
part, or an API mismatch), coincidence is INFERRED from meeting line endpoints so profiles
still close — the original fallback, now a safety net rather than the only path.

UNVERIFIED: needs a real NX session (the constraint/geometry reads follow the documented
NXOpen API but can only be confirmed live).
"""
from __future__ import annotations

import math
from typing import Dict, List

from ..model.sketch import (
    NxConstraintKind,
    NxCurveKind,
    NxCurvePointRole,
    NxPointRef,
    NxSketch,
    NxSketchConstraint,
)
from . import sketch_constraint_reader, sketch_geometry

_COINCIDENCE_TOL = 1e-4  # mm, in sketch 2D


def extract(part, document, curve_tag_to_sketch: Dict[int, int], report) -> None:
    for sketch in part.Sketches:
        geometry = list(sketch.GetAllGeometry())
        extracted, tag_to_curve_id = sketch_geometry.read(sketch, geometry)
        if extracted is None:
            continue

        read = sketch_constraint_reader.read_constraints(sketch, extracted, tag_to_curve_id, report)
        if read == 0:
            _infer_coincidences(extracted)  # fallback so profiles still close
        sketch_constraint_reader.read_dimensions(sketch, extracted, tag_to_curve_id, report)

        index = len(document.sketches)
        document.sketches.append(extracted)
        # Map this sketch's curves so a feature's section resolves to its sketch index.
        for obj in geometry:
            curve_tag_to_sketch[obj.Tag] = index


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


def _distance_2d(a, b) -> float:
    dx = a[0] - b[0]
    dy = a[1] - b[1]
    return math.sqrt(dx * dx + dy * dy)
