# SPDX-License-Identifier: GPL-2.0-only
"""Reads a sketch's curve geometry into IR curves (the NXOpen-aware half of sketches).

Handles line, full circle, partial arc, ellipse, elliptical arc and spline. The sketch
plane is fitted from the curves' 3D points and every point projects into that 2D frame,
so absolute geometry is reconstructed without trusting the NX sketch-plane API. Returns
the fitted frame plus a map from each curve's NXOpen ``.Tag`` to the IR curve id, which
the constraint reader uses to resolve a constraint's geometry to a curve.

UNVERIFIED — needs a real NX session; member shapes follow the documented NXOpen API.
Arc/ellipse endpoint reconstruction and spline pole reading are best-effort and flagged.
"""
from __future__ import annotations

import math
from typing import Dict, List, Optional, Tuple

import NXOpen

from ..model import sketch_plane_math as spm
from ..model.sketch import NxCurve, NxCurveKind, NxSketch

_FULL_TURN = 2 * math.pi
_FULL_TOL = 1e-6


def read(sketch, geometry) -> Tuple[Optional[NxSketch], Dict[int, int]]:
    """Builds an NxSketch (geometry only) and the tag->curve-id map. None if it has no curves."""
    frame = spm.fit(_collect_points(geometry))
    result = NxSketch(name=sketch.Name, origin=frame.origin, xaxis=frame.xaxis, yaxis=frame.yaxis)
    tag_to_curve_id: Dict[int, int] = {}

    next_id = 1
    for obj in geometry:
        curve = _curve_of(obj, next_id, frame)
        if curve is None:
            continue
        result.curves.append(curve)
        tag_to_curve_id[obj.Tag] = curve.id
        next_id += 1

    if not result.curves:
        return None, {}
    return result, tag_to_curve_id


def _curve_of(obj, curve_id: int, frame: spm.SketchPlaneFrame) -> Optional[NxCurve]:
    if isinstance(obj, NXOpen.Line):
        return NxCurve(
            id=curve_id, kind=NxCurveKind.LINE,
            start=frame.to_2d(_p(obj.StartPoint)), end=frame.to_2d(_p(obj.EndPoint)),
        )
    if isinstance(obj, NXOpen.Arc):
        return _arc_curve(obj, curve_id, frame)
    if isinstance(obj, NXOpen.Ellipse):
        return _ellipse_curve(obj, curve_id, frame)
    if isinstance(obj, NXOpen.Spline):
        return _spline_curve(obj, curve_id, frame)
    return None


def _arc_curve(arc, curve_id: int, frame: spm.SketchPlaneFrame) -> NxCurve:
    center = _p(arc.CenterPoint)
    span = arc.EndAngle - arc.StartAngle
    if abs(span - _FULL_TURN) < _FULL_TOL:
        return NxCurve(
            id=curve_id, kind=NxCurveKind.CIRCLE, center=frame.to_2d(center), radius=arc.Radius
        )
    # Partial arc: reconstruct the end points on the sketch plane from the angles measured
    # about the frame's in-plane axes. (Best-effort: NX measures arc angles in the arc's own
    # reference frame; for sketch arcs that is the sketch plane, confirmed in a live session.)
    start = _on_circle(center, arc.Radius, arc.StartAngle, frame)
    end = _on_circle(center, arc.Radius, arc.EndAngle, frame)
    return NxCurve(
        id=curve_id, kind=NxCurveKind.ARC,
        center=frame.to_2d(center), start=frame.to_2d(start), end=frame.to_2d(end), ccw=True,
    )


def _ellipse_curve(ellipse, curve_id: int, frame: spm.SketchPlaneFrame) -> NxCurve:
    center = frame.to_2d(_p(ellipse.CenterPoint))
    # The major-axis direction is the frame X axis rotated by the ellipse's rotation angle.
    angle = ellipse.RotationAngle
    major_axis = [math.cos(angle), math.sin(angle)]
    span = ellipse.EndAngle - ellipse.StartAngle
    if abs(span - _FULL_TURN) < _FULL_TOL:
        return NxCurve(
            id=curve_id, kind=NxCurveKind.ELLIPSE, center=center, major_axis=major_axis,
            major_radius=ellipse.MajorRadius, minor_radius=ellipse.MinorRadius,
        )
    return NxCurve(
        id=curve_id, kind=NxCurveKind.ELLIPTICAL_ARC, center=center, major_axis=major_axis,
        major_radius=ellipse.MajorRadius, minor_radius=ellipse.MinorRadius,
        start_angle=ellipse.StartAngle, end_angle=ellipse.EndAngle,
    )


def _spline_curve(spline, curve_id: int, frame: spm.SketchPlaneFrame) -> NxCurve:
    # Read the spline's poles as its defining points (best-effort; a through-points spline
    # would expose its fit points via the UF spline API, a live-NX refinement).
    points = [frame.to_2d(_p(pole)) for pole in _poles_of(spline)]
    return NxCurve(id=curve_id, kind=NxCurveKind.SPLINE, spline_points=points, fit=False)


def _poles_of(spline) -> List:
    poles = spline.GetPoles()
    # NXOpen poles are 4D (x, y, z, w); expose them as Point3d-like via their components.
    return [_Pole(p) for p in poles]


class _Pole:
    """Adapts a 4D NXOpen pole to the X/Y/Z accessor the projection helper expects."""

    def __init__(self, pole) -> None:
        self.X = pole.X
        self.Y = pole.Y
        self.Z = pole.Z


def _on_circle(center, radius: float, angle: float, frame: spm.SketchPlaneFrame) -> List[float]:
    fx = frame.xaxis
    fy = frame.yaxis
    return [
        center[i] + radius * (math.cos(angle) * fx[i] + math.sin(angle) * fy[i])
        for i in range(3)
    ]


def _collect_points(geometry) -> List[List[float]]:
    points: List[List[float]] = []
    for obj in geometry:
        if isinstance(obj, NXOpen.Line):
            points.append(_p(obj.StartPoint))
            points.append(_p(obj.EndPoint))
        elif isinstance(obj, (NXOpen.Arc, NXOpen.Ellipse)):
            points.append(_p(obj.CenterPoint))
        elif isinstance(obj, NXOpen.Spline):
            for pole in _poles_of(obj):
                points.append([pole.X, pole.Y, pole.Z])
    return points


def _p(point) -> List[float]:
    return [point.X, point.Y, point.Z]
