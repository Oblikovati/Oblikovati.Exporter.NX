# SPDX-License-Identifier: GPL-2.0-only
"""NX-neutral datum work-plane IR (a frozen frame). Lengths are millimetres."""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import List


@dataclass
class NxWorkPlane:
    """A datum plane captured as a frozen frame: origin + two in-plane axes (mm).

    Maps to a fixed-frame work plane, carrying the datum faithfully without re-deriving
    its NX construction.
    """

    name: str = ""
    origin: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    xaxis: List[float] = field(default_factory=lambda: [1.0, 0.0, 0.0])
    yaxis: List[float] = field(default_factory=lambda: [0.0, 1.0, 0.0])
