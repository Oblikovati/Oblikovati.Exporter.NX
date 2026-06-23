# SPDX-License-Identifier: GPL-2.0-only
"""Builds an OccurrenceData from an NX occurrence and its component's file name.

The placement becomes a 16-cell row-major transform with the rotation in the upper-left
3x3 and the translation (mm -> cm) in cells 3, 7, 11 — the layout math.Matrix4 uses.
"""
from __future__ import annotations

from typing import List

from ..model.document import NxOccurrence
from ..recipe.document import OccurrenceData
from .units import MM_TO_CM


def translate(occurrence: NxOccurrence, component_file_name: str) -> OccurrenceData:
    return OccurrenceData(
        name=occurrence.name,
        component=component_file_name,
        transform=_build_transform(occurrence.rotation, occurrence.position),
    )


def _build_transform(r: List[float], p: List[float]) -> List[float]:
    return [
        r[0], r[1], r[2], p[0] * MM_TO_CM,
        r[3], r[4], r[5], p[1] * MM_TO_CM,
        r[6], r[7], r[8], p[2] * MM_TO_CM,
        0.0, 0.0, 0.0, 1.0,
    ]
