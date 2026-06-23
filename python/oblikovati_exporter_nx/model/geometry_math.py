# SPDX-License-Identifier: GPL-2.0-only
"""Small pure vector helpers for building geometric descriptors from NX vertex data.

Kept free of NXOpen so the descriptor math is unit-testable (the surrounding extraction
that reads NXOpen is not).
"""
from __future__ import annotations

import math
from typing import List, Sequence


def midpoint(a: Sequence[float], b: Sequence[float]) -> List[float]:
    """Midpoint of two 3D points."""
    return [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2]


def average(points: Sequence[Sequence[float]]) -> List[float]:
    """Component-wise average of 3D points (the empty set averages to the origin)."""
    if not points:
        return [0.0, 0.0, 0.0]
    x = y = z = 0.0
    for p in points:
        x += p[0]
        y += p[1]
        z += p[2]
    n = len(points)
    return [x / n, y / n, z / n]


def unit_direction(from_point: Sequence[float], to_point: Sequence[float]) -> List[float]:
    """Unit vector from ``from_point`` to ``to_point``; the zero vector when they coincide."""
    dx = to_point[0] - from_point[0]
    dy = to_point[1] - from_point[1]
    dz = to_point[2] - from_point[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz)
    if length == 0:
        return [0.0, 0.0, 0.0]
    return [dx / length, dy / length, dz / length]
