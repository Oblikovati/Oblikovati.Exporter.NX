# SPDX-License-Identifier: GPL-2.0-only
"""NX-neutral feature IR. The translator dispatches on the concrete dataclass type."""
from __future__ import annotations

import enum
from dataclasses import dataclass, field
from typing import List


class NxOperation(enum.Enum):
    """Boolean operation a feature performs against existing bodies."""

    NEW_BODY = "newBody"
    JOIN = "join"
    CUT = "cut"
    INTERSECT = "intersect"


class NxExtentDirection(enum.Enum):
    """Which way a single-distance extent grows from its sketch plane."""

    POSITIVE = "positive"
    NEGATIVE = "negative"
    SYMMETRIC = "symmetric"


@dataclass
class NxFeature:
    """Base of an extracted NX feature."""

    name: str = ""


@dataclass
class NxExtrude(NxFeature):
    """An extrude of a sketch profile. Lengths are millimetres (IR contract)."""

    sketch_index: int = 0
    profile_index: int = 0
    operation: NxOperation = NxOperation.NEW_BODY
    direction: NxExtentDirection = NxExtentDirection.POSITIVE
    distance: float = 0.0
    second_distance: float = 0.0  # asymmetric two-sided extrude (mm)
    taper_degrees: float = 0.0


@dataclass
class NxRevolve(NxFeature):
    """A revolve about the sketch's own centerline. ``angle_degrees`` 0 = full revolution."""

    sketch_index: int = 0
    profile_index: int = 0
    operation: NxOperation = NxOperation.NEW_BODY
    angle_degrees: float = 0.0


@dataclass
class NxReplicatingFeature(NxFeature):
    """Base of features that replicate earlier features by IR feature index."""

    source_feature_indices: List[int] = field(default_factory=list)


@dataclass
class NxRectangularPattern(NxReplicatingFeature):
    """A rectangular grid pattern. Step vectors are the offset between adjacent copies (mm)."""

    count_x: int = 1
    count_y: int = 1
    step_x: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    step_y: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])


@dataclass
class NxCircularPattern(NxReplicatingFeature):
    """A circular pattern about an axis. ``angle_degrees`` is the total spread (0 = full 360)."""

    count: int = 1
    angle_degrees: float = 0.0
    axis_point: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    axis_dir: List[float] = field(default_factory=lambda: [0.0, 0.0, 1.0])


@dataclass
class NxMirror(NxReplicatingFeature):
    """A mirror across a plane given by its origin (mm) and unit normal."""

    plane_origin: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    plane_normal: List[float] = field(default_factory=lambda: [1.0, 0.0, 0.0])
