# SPDX-License-Identifier: GPL-2.0-only
"""Feature recipe payloads. Mirrors FeatureData + serialize_*.go payload shapes.

A feature is a ``kind`` discriminator plus exactly one typed payload emitted under its
own alias (extrude / revolve / rectangularPattern / ...). Distances are centimetres;
angles are radians.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional


@dataclass
class FeatureData:
    """One history feature: a kind, an optional name, and one aliased payload."""

    kind: str = ""
    name: Optional[str] = None
    payload_alias: str = ""
    payload: Optional[Any] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"kind": self.kind}
        if self.name is not None:
            body["name"] = self.name
        if self.payload is not None:
            body[self.payload_alias] = self.payload.to_yaml()
        return body


@dataclass
class GeomEdgeRefData:
    """Geometric edge descriptor (ADR-0040): midpoint + direction (cm)."""

    midpoint: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    direction: Optional[List[float]] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"midpoint": list(self.midpoint)}
        if self.direction is not None:
            body["direction"] = list(self.direction)
        return body


@dataclass
class GeomFaceRefData:
    """Geometric face descriptor (ADR-0040): centroid + outward normal (cm)."""

    centroid: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    normal: Optional[List[float]] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"centroid": list(self.centroid)}
        if self.normal is not None:
            body["normal"] = list(self.normal)
        return body


@dataclass
class EdgeDressData:
    """Edge dress-up (fillet radius / chamfer distance). Edges are geometric descriptors."""

    value: float = 0.0
    geom_edges: List[GeomEdgeRefData] = field(default_factory=list)

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"value": self.value}
        if self.geom_edges:
            body["geomEdges"] = [e.to_yaml() for e in self.geom_edges]
        return body


@dataclass
class FaceDressData:
    """Face dress-up (shell thickness / draft angle). Faces are geometric descriptors."""

    value: float = 0.0
    pull: Optional[List[float]] = None
    geom_faces: List[GeomFaceRefData] = field(default_factory=list)

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"value": self.value}
        if self.pull is not None:
            body["pull"] = list(self.pull)
        if self.geom_faces:
            body["geomFaces"] = [f.to_yaml() for f in self.geom_faces]
        return body


@dataclass
class HoleData:
    """A hole. The placement face is a geometric descriptor. Diameter/depth are cm.

    ``center`` is the explicit drill point in model space (cm); the kernel projects it onto
    the placement face's plane. Omitted (None) means drill at the face centroid.
    """

    diameter: float = 0.0
    depth: float = 0.0
    through_all: Optional[bool] = None
    type: str = "drilled"
    geom_face: Optional[GeomFaceRefData] = None
    center: Optional[List[float]] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"diameter": self.diameter, "depth": self.depth}
        if self.through_all is not None:
            body["throughAll"] = self.through_all
        body["type"] = self.type
        if self.geom_face is not None:
            body["geomFace"] = self.geom_face.to_yaml()
        if self.center is not None:
            body["center"] = list(self.center)
        return body


@dataclass
class ExtrudeData:
    """A sketch-profile extrude. Sketch is the array index; profiles are region indices."""

    sketch: int = 0
    profiles: List[int] = field(default_factory=list)
    operation: str = "newBody"
    extent: Optional[str] = None
    direction: Optional[str] = None
    distance: Optional[float] = None
    distance2: Optional[float] = None
    taper: Optional[float] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"sketch": self.sketch}
        if self.profiles:
            body["profiles"] = list(self.profiles)
        body["operation"] = self.operation
        if self.extent is not None:
            body["extent"] = self.extent
        if self.direction is not None:
            body["direction"] = self.direction
        if self.distance is not None:
            body["distance"] = self.distance
        if self.distance2 is not None:
            body["distance2"] = self.distance2
        if self.taper is not None:
            body["taper"] = self.taper
        return body


@dataclass
class SweepData:
    """A sweep: a profile (sketch + region) swept along a 3D-point path polyline (cm)."""

    sketch: int = 0
    profile: int = 0
    path: List[List[float]] = field(default_factory=list)
    closed: Optional[bool] = None
    operation: str = "newBody"

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"sketch": self.sketch, "profile": self.profile}
        body["path"] = [list(p) for p in self.path]
        if self.closed is not None:
            body["closed"] = self.closed
        body["operation"] = self.operation
        return body


@dataclass
class LoftSectionData:
    """One loft section: a profile (sketch + region index)."""

    sketch: int = 0
    profile: int = 0

    def to_yaml(self) -> Dict[str, Any]:
        return {"sketch": self.sketch, "profile": self.profile}


@dataclass
class LoftData:
    """A loft through an ordered list of profile sections."""

    sections: List[LoftSectionData] = field(default_factory=list)
    closed: Optional[bool] = None
    operation: str = "newBody"

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"sections": [s.to_yaml() for s in self.sections]}
        if self.closed is not None:
            body["closed"] = self.closed
        body["operation"] = self.operation
        return body


@dataclass
class RevolveData:
    """A revolve about the profile sketch's own centerline. Angle in radians; None = full."""

    sketch: int = 0
    profile: int = 0
    angle: Optional[float] = None
    operation: str = "newBody"

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"sketch": self.sketch, "profile": self.profile}
        if self.angle is not None:
            body["angle"] = self.angle
        body["operation"] = self.operation
        return body


@dataclass
class RectPatternData:
    """A rectangular grid pattern. Source are program indices; steps are cm offsets."""

    source: List[int] = field(default_factory=list)
    count_x: int = 0
    count_y: int = 0
    step_x: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    step_y: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])

    def to_yaml(self) -> Dict[str, Any]:
        return {
            "source": list(self.source),
            "countX": self.count_x,
            "countY": self.count_y,
            "stepX": list(self.step_x),
            "stepY": list(self.step_y),
        }


@dataclass
class CircPatternData:
    """A circular pattern about an axis. Angle in radians."""

    source: List[int] = field(default_factory=list)
    count: int = 0
    angle: float = 0.0
    axis_point: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    axis_dir: List[float] = field(default_factory=lambda: [0.0, 0.0, 1.0])

    def to_yaml(self) -> Dict[str, Any]:
        return {
            "source": list(self.source),
            "count": self.count,
            "angle": self.angle,
            "axisPoint": list(self.axis_point),
            "axisDir": list(self.axis_dir),
        }


@dataclass
class MirrorData:
    """A mirror across a plane (origin + normal). Source are program indices."""

    source: List[int] = field(default_factory=list)
    plane: str = ""
    origin: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    normal: List[float] = field(default_factory=lambda: [1.0, 0.0, 0.0])

    def to_yaml(self) -> Dict[str, Any]:
        return {
            "source": list(self.source),
            "plane": self.plane,
            "origin": list(self.origin),
            "normal": list(self.normal),
        }
