# SPDX-License-Identifier: GPL-2.0-only
"""Tessellates a sweep guide (an NX Section) into a 3D-point polyline (model space, mm).

Oblikovati stores a sweep's path as explicit points, not a curve reference, so the guide's
curves are reduced to a polyline: a line contributes its two endpoints; any other curve
(arc, spline) is sampled with the UF curve evaluator — NX's general curve tessellator, the
counterpart of the Inventor exporter's CurveEvaluator.GetStrokes. Consecutive coincident
points are de-duplicated so segments join cleanly.

UNVERIFIED — needs a real NX session; the UF Eval call shapes follow the documented API
and must be confirmed live (arc/spline path fidelity in particular).
"""
from __future__ import annotations

from typing import List

import NXOpen

_SEGMENTS = 24  # samples per non-linear guide curve
_DEDUP_TOL = 1e-6  # mm


def polyline(section) -> List[List[float]]:
    points: List[List[float]] = []
    ufs = NXOpen.UF.UFSession.GetUFSession()
    for curve in section.GetOutputCurves():
        for point in _tessellate(ufs, curve):
            _append(points, point)
    return points


def _tessellate(ufs, curve) -> List[List[float]]:
    if isinstance(curve, NXOpen.Line):
        return [_p(curve.StartPoint), _p(curve.EndPoint)]
    evaluator = ufs.Eval.Initialize(curve.Tag)
    try:
        start, end = ufs.Eval.AskLimits(evaluator)
        out: List[List[float]] = []
        for i in range(_SEGMENTS + 1):
            t = start + (end - start) * i / _SEGMENTS
            point = ufs.Eval.Evaluate(evaluator, 0, t)[0]
            out.append([point[0], point[1], point[2]])
        return out
    finally:
        ufs.Eval.Free(evaluator)


def _append(points: List[List[float]], point: List[float]) -> None:
    if points and _close(points[-1], point):
        return
    points.append(point)


def _close(a: List[float], b: List[float]) -> bool:
    return all(abs(a[i] - b[i]) <= _DEDUP_TOL for i in range(3))


def _p(point) -> List[float]:
    return [point.X, point.Y, point.Z]
