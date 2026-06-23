# SPDX-License-Identifier: GPL-2.0-only
"""Adds the NX revolve axis to its profile sketch as a centerline.

Oblikovati revolves about the sketch's own centerline (a line flagged ``centerline``),
so the axis — a point + direction in model space — is projected into the sketch's fitted
frame and added as a 2D centerline line. A no-op when the axis is not in-plane (its
projection degenerates to a point), which leaves the revolve to fail honestly.
"""
from __future__ import annotations

import math

from ..model import sketch_plane_math as spm
from ..model.sketch import NxCurve, NxCurveKind, NxSketch

_AXIS_HALF_LENGTH = 500.0  # mm; a centerline is an axis, length is cosmetic


def inject(sketch: NxSketch, axis) -> None:
    frame = spm.SketchPlaneFrame(origin=sketch.origin, xaxis=sketch.xaxis, yaxis=sketch.yaxis)
    point = [axis.Point.X, axis.Point.Y, axis.Point.Z]
    direction = spm.normalize([axis.Direction.X, axis.Direction.Y, axis.Direction.Z])

    a = frame.to_2d(spm.sub(point, spm.scale(direction, _AXIS_HALF_LENGTH)))
    b = frame.to_2d(spm.sub(point, spm.scale(direction, -_AXIS_HALF_LENGTH)))
    if _distance_2d(a, b) < 1e-6:
        return  # axis is perpendicular to the sketch — not a usable in-plane centerline

    sketch.curves.append(
        NxCurve(id=_next_curve_id(sketch), kind=NxCurveKind.LINE, start=a, end=b, centerline=True)
    )


def _next_curve_id(sketch: NxSketch) -> int:
    return max((c.id for c in sketch.curves), default=0) + 1


def _distance_2d(a, b) -> float:
    dx = a[0] - b[0]
    dy = a[1] - b[1]
    return math.sqrt(dx * dx + dy * dy)
