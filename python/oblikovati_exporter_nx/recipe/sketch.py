# SPDX-License-Identifier: GPL-2.0-only
"""Sketch recipe model. Mirrors SketchData in Oblikovati/model/sketch/serialize.go.

Coincidence is modelled by explicit ``coincident`` constraints between distinct point
ids (each curve keeps its own endpoint/center points), matching how the engine itself
serializes sketches. Coordinates are centimetres (database units).
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional


@dataclass
class PlaneData:
    """Sketch plane as origin + two in-plane axes (model space)."""

    origin: List[float] = field(default_factory=lambda: [0.0, 0.0, 0.0])
    xaxis: List[float] = field(default_factory=lambda: [1.0, 0.0, 0.0])
    yaxis: List[float] = field(default_factory=lambda: [0.0, 1.0, 0.0])

    def to_yaml(self) -> Dict[str, Any]:
        return {"origin": list(self.origin), "xAxis": list(self.xaxis), "yAxis": list(self.yaxis)}


@dataclass
class PointData:
    """One constrainable point. ``standalone`` marks a SketchPoint entity."""

    id: int = 0
    x: float = 0.0
    y: float = 0.0
    standalone: Optional[bool] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"id": self.id, "x": self.x, "y": self.y}
        if self.standalone is not None:
            body["standalone"] = self.standalone
        return body


@dataclass
class EntityData:
    """One curve entity. ``points`` lists defining point ids in a kind-specific order."""

    id: int = 0
    kind: str = ""
    points: List[int] = field(default_factory=list)
    radius: Optional[float] = None
    ccw: Optional[bool] = None
    construction: Optional[bool] = None
    centerline: Optional[bool] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"id": self.id, "kind": self.kind}
        if self.points:
            body["points"] = list(self.points)
        if self.radius is not None:
            body["radius"] = self.radius
        if self.ccw is not None:
            body["ccw"] = self.ccw
        if self.construction is not None:
            body["construction"] = self.construction
        if self.centerline is not None:
            body["centerline"] = self.centerline
        return body


@dataclass
class ConstraintData:
    """One geometric constraint. Operands split into point ids and curve ids."""

    kind: str = ""
    points: List[int] = field(default_factory=list)
    curves: List[int] = field(default_factory=list)

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"kind": self.kind}
        if self.points:
            body["points"] = list(self.points)
        if self.curves:
            body["curves"] = list(self.curves)
        return body


@dataclass
class DimensionData:
    """One dimensional constraint linking geometry to a parameter expression."""

    kind: str = ""
    points: List[int] = field(default_factory=list)
    curves: List[int] = field(default_factory=list)
    expression: str = ""  # the Go field has no omitempty: always written
    driven: Optional[bool] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"kind": self.kind}
        if self.points:
            body["points"] = list(self.points)
        if self.curves:
            body["curves"] = list(self.curves)
        body["expression"] = self.expression
        if self.driven is not None:
            body["driven"] = self.driven
        return body


@dataclass
class SketchData:
    """One 2D sketch."""

    id: int = 0
    name: Optional[str] = None
    plane: PlaneData = field(default_factory=PlaneData)
    points: List[PointData] = field(default_factory=list)
    entities: List[EntityData] = field(default_factory=list)
    constraints: List[ConstraintData] = field(default_factory=list)
    dimensions: List[DimensionData] = field(default_factory=list)

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"id": self.id}
        if self.name is not None:
            body["name"] = self.name
        body["plane"] = self.plane.to_yaml()
        if self.points:
            body["points"] = [p.to_yaml() for p in self.points]
        if self.entities:
            body["entities"] = [e.to_yaml() for e in self.entities]
        if self.constraints:
            body["constraints"] = [c.to_yaml() for c in self.constraints]
        if self.dimensions:
            body["dimensions"] = [d.to_yaml() for d in self.dimensions]
        return body
