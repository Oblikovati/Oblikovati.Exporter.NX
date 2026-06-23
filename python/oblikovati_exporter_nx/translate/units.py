# SPDX-License-Identifier: GPL-2.0-only
"""Shared unit conversions. The kernel/recipe database unit is the centimetre."""
from __future__ import annotations

import math
from typing import List, Sequence

MM_TO_CM = 0.1
DEG_TO_RAD = math.pi / 180.0


def scale_point(v: Sequence[float]) -> List[float]:
    """Scales a 3D point from millimetres to centimetres."""
    return [v[0] * MM_TO_CM, v[1] * MM_TO_CM, v[2] * MM_TO_CM]
