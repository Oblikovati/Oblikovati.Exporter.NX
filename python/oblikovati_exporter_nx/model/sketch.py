# SPDX-License-Identifier: GPL-2.0-only
"""NX-neutral sketch IR: a plane, curves, geometric constraints and dimensions.

All lengths are in MILLIMETRES (the IR contract). The translator turns this into
Oblikovati's shared-point sketch model.
"""
from __future__ import annotations

import enum
from dataclasses import dataclass, field
from typing import List


class NxCurveKind(enum.Enum):
    LINE = "line"
    CIRCLE = "circle"
    ARC = "arc"


class NxCurvePointRole(enum.Enum):
    """Which defining point of a curve a constraint/dimension refers to."""

    START = "start"
    END = "end"
    CENTER = "center"


@dataclass(frozen=True)
class NxPointRef:
    """A reference to one defining point of a curve (e.g. a line's end point)."""

    curve_id: int
    role: NxCurvePointRole


@dataclass
class NxCurve:
    """One sketch curve. Coordinates are 2D in sketch space (mm).

    A line uses start/end; a circle uses center/radius; an arc uses center/start/end
    plus ccw.
    """

    id: int = 0
    kind: NxCurveKind = NxCurveKind.LINE
    start: List[float] = field(default_factory=lambda: [0.0, 0.0])
    end: List[float] = field(default_factory=lambda: [0.0, 0.0])
    center: List[float] = field(default_factory=lambda: [0.0, 0.0])
    radius: float = 0.0
    ccw: bool = False
    construction: bool = False
    # A line that acts as an axis (excluded from profiles; used as a revolve axis).
    centerline: bool = False


class NxConstraintKind(enum.Enum):
    COINCIDENT = "coincident"
    HORIZONTAL = "horizontal"
    VERTICAL = "vertical"
    PARALLEL = "parallel"
    PERPENDICULAR = "perpendicular"
    COLLINEAR = "collinear"
    EQUAL_LENGTH = "equalLength"
    CONCENTRIC = "concentric"
    EQUAL_RADIUS = "equalRadius"
    TANGENT = "tangent"
    POINT_ON_LINE = "pointOnLine"
    MIDPOINT = "midpoint"
    FIX = "fix"


@dataclass
class NxSketchConstraint:
    """One geometric constraint. ``points`` carries point-ref operands; ``curves`` carries curve ids."""

    kind: NxConstraintKind = NxConstraintKind.COINCIDENT
    points: List[NxPointRef] = field(default_factory=list)
    curves: List[int] = field(default_factory=list)


class NxDimensionKind(enum.Enum):
    DISTANCE = "distance"
    RADIUS = "radius"
    DIAMETER = "diameter"
    ANGLE = "angle"


@dataclass
class NxSketchDimension:
    """One dimensional constraint. ``expression`` drives it; ``driven`` measures instead."""

    kind: NxDimensionKind = NxDimensionKind.DISTANCE
    points: List[NxPointRef] = field(default_factory=list)
    curves: List[int] = field(default_factory=list)
    expression: str = ""
    driven: bool = False


@dataclass
class NxSketch:
    """An NX sketch in NX-neutral terms: a plane plus curves, constraints and dimensions."""

    name: str = ""
    origin: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    xaxis: List[float] = field(default_factory=lambda: [1.0, 0.0, 0.0])
    yaxis: List[float] = field(default_factory=lambda: [0.0, 1.0, 0.0])
    curves: List[NxCurve] = field(default_factory=list)
    constraints: List[NxSketchConstraint] = field(default_factory=list)
    dimensions: List[NxSketchDimension] = field(default_factory=list)
