# SPDX-License-Identifier: GPL-2.0-only
"""NX-neutral dress-up IR (fillet/chamfer/shell/draft/hole) + geometric descriptors.

The adapter computes descriptors from NX edges/faces; Oblikovati binds them to body
edges/faces on recompute (ADR-0040), so the exporter needs no Oblikovati lineage keys.
Lengths are millimetres (the IR contract).
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import List

from .feature import NxFeature


@dataclass
class NxEdgeDescriptor:
    """A geometric edge descriptor: midpoint + direction in model space (mm)."""

    midpoint: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    direction: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])


@dataclass
class NxFaceDescriptor:
    """A geometric face descriptor: centroid + outward normal (mm / unit)."""

    centroid: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    normal: List[float] = field(default_factory=lambda: [0.0, 0.0, 1.0])


@dataclass
class NxFillet(NxFeature):
    """A fillet rounding the given edges to ``radius_mm``."""

    edges: List[NxEdgeDescriptor] = field(default_factory=list)
    radius_mm: float = 0.0


@dataclass
class NxChamfer(NxFeature):
    """A chamfer bevelling the given edges by ``distance_mm`` (equal distance)."""

    edges: List[NxEdgeDescriptor] = field(default_factory=list)
    distance_mm: float = 0.0


@dataclass
class NxShell(NxFeature):
    """A shell hollowing the body, removing the given faces, to ``thickness_mm``."""

    removed_faces: List[NxFaceDescriptor] = field(default_factory=list)
    thickness_mm: float = 0.0


@dataclass
class NxDraft(NxFeature):
    """A draft tapering the given faces by ``angle_degrees`` about a pull direction."""

    faces: List[NxFaceDescriptor] = field(default_factory=list)
    angle_degrees: float = 0.0
    pull: List[float] = field(default_factory=lambda: [0.0, 0.0, 1.0])


@dataclass
class NxHole(NxFeature):
    """A drilled hole on a placement face (geometric descriptor)."""

    placement_face: NxFaceDescriptor = field(default_factory=NxFaceDescriptor)
    diameter_mm: float = 0.0
    depth_mm: float = 0.0
    through_all: bool = False
