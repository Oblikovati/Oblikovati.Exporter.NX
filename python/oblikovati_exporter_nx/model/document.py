# SPDX-License-Identifier: GPL-2.0-only
"""The NX-neutral root document and expression IR.

The NXOpen adapter populates this from a live session; the translator consumes only
this (never NXOpen). All lengths in the IR are millimetres (the adapter normalises NX's
part units); the translator converts to the recipe's centimetre database unit.
"""
from __future__ import annotations

import enum
from dataclasses import dataclass, field
from typing import List

from .feature import NxFeature
from .sketch import NxSketch
from .workfeature import NxWorkPlane


class NxDocumentKind(enum.IntEnum):
    """Kind of an extracted NX document (mirrors Oblikovati's document types)."""

    PART = 1
    ASSEMBLY = 2


@dataclass
class NxExpression:
    """One NX expression (the NX equivalent of an Oblikovati parameter).

    ``formula`` is the raw NX right-hand side and may reference other expressions by
    name (e.g. ``"width * 2"``). ``unit`` is the abbreviation NX associates with it.
    """

    name: str = ""
    formula: str = ""
    unit: str = ""


@dataclass
class NxOccurrence:
    """One placed component in an assembly.

    The referenced ``component`` document is shared by reference so instances dedup to
    one exported file. ``position`` is the translation (mm); ``rotation`` is a row-major
    3x3 (identity by default).
    """

    name: str = ""
    component: "NxDocument" = None  # type: ignore[assignment]
    position: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    rotation: List[float] = field(default_factory=lambda: [1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0])


@dataclass
class NxDocument:
    """The NX-neutral root of one extracted document."""

    display_name: str = ""
    kind: NxDocumentKind = NxDocumentKind.PART
    length_unit: str = "mm"
    angle_unit: str = "deg"
    expressions: List[NxExpression] = field(default_factory=list)
    work_planes: List[NxWorkPlane] = field(default_factory=list)
    sketches: List[NxSketch] = field(default_factory=list)
    features: List[NxFeature] = field(default_factory=list)
    occurrences: List[NxOccurrence] = field(default_factory=list)
