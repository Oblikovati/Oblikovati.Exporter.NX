# SPDX-License-Identifier: GPL-2.0-only
"""Pure geometry for sketch extraction: fit a plane frame to 3D points, project into it.

Kept NXOpen-free so it is unit-testable (the surrounding extraction that reads NXOpen
curves is not). Robust enough for the planar sketches NX produces; a non-planar point
set fits to its first valid triangle.
"""
from __future__ import annotations

import math
from dataclasses import dataclass, field
from typing import List, Optional, Sequence

from . import geometry_math


def sub(a: Sequence[float], b: Sequence[float]) -> List[float]:
    return [a[0] - b[0], a[1] - b[1], a[2] - b[2]]


def dot(a: Sequence[float], b: Sequence[float]) -> float:
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def cross(a: Sequence[float], b: Sequence[float]) -> List[float]:
    return [
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    ]


def scale(a: Sequence[float], s: float) -> List[float]:
    return [a[0] * s, a[1] * s, a[2] * s]


def length(a: Sequence[float]) -> float:
    return math.sqrt(dot(a, a))


def normalize(a: Sequence[float]) -> List[float]:
    ln = length(a)
    return [0.0, 0.0, 0.0] if ln == 0 else scale(a, 1 / ln)


@dataclass
class SketchPlaneFrame:
    """A fitted sketch plane frame: origin + orthonormal in-plane axes (model space)."""

    origin: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    xaxis: List[float] = field(default_factory=lambda: [1.0, 0.0, 0.0])
    yaxis: List[float] = field(default_factory=lambda: [0.0, 1.0, 0.0])

    def to_2d(self, p: Sequence[float]) -> List[float]:
        """Projects a model-space point onto the frame's 2D (u, v) coordinates."""
        d = sub(p, self.origin)
        return [dot(d, self.xaxis), dot(d, self.yaxis)]


def fit(points: Sequence[Sequence[float]]) -> SketchPlaneFrame:
    """Fit a plane frame to points.

    Origin at their centroid, normal from the first non-degenerate triangle, X axis
    toward the farthest in-plane point, Y = normal x X. Falls back to the world XY frame
    when the points are collinear/degenerate.
    """
    origin = geometry_math.average(points)
    normal = _first_normal(origin, points)
    if normal is None:
        return SketchPlaneFrame(origin=origin)
    x = _farthest_in_plane(origin, normal, points)
    y = normalize(cross(normal, x))
    return SketchPlaneFrame(origin=origin, xaxis=x, yaxis=y)


def _first_normal(origin: Sequence[float], points: Sequence[Sequence[float]]) -> Optional[List[float]]:
    for i in range(len(points)):
        for j in range(i + 1, len(points)):
            n = cross(sub(points[i], origin), sub(points[j], origin))
            if length(n) > 1e-9:
                return normalize(n)
    return None


def _farthest_in_plane(
    origin: Sequence[float], normal: Sequence[float], points: Sequence[Sequence[float]]
) -> List[float]:
    best = [1.0, 0.0, 0.0]
    best_len = 0.0
    for p in points:
        d = sub(p, origin)
        in_plane = sub(d, scale(normal, dot(d, normal)))
        ln = length(in_plane)
        if ln > best_len:
            best_len = ln
            best = in_plane
    return normalize(best) if best_len > 1e-9 else _any_perpendicular(normal)


def _any_perpendicular(n: Sequence[float]) -> List[float]:
    seed = [1.0, 0.0, 0.0] if abs(n[0]) < 0.9 else [0.0, 1.0, 0.0]
    return normalize(cross(n, seed))
