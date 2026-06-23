# SPDX-License-Identifier: GPL-2.0-only
"""Recipe document envelopes: the serialized root plus part/assembly bodies.

Field names and shape mirror the Go codec in Oblikovati/model/compdef/serialize.go and
assembly_serialize.go. Each class exposes ``to_yaml()`` returning a plain dict whose key
order and presence match what the C# YamlDotNet writer emits (only-present-keys), so the
two exporters produce identical bytes for the same input.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional


@dataclass
class Units:
    """Display/exchange unit preferences. The kernel works internally in centimetres."""

    length: str = "mm"
    angle: str = "deg"
    area: str = "mm^2"
    volume: str = "mm^3"
    mass: str = "kg"
    time: str = "s"

    def to_yaml(self) -> Dict[str, Any]:
        return {
            "length": self.length,
            "angle": self.angle,
            "area": self.area,
            "volume": self.volume,
            "mass": self.mass,
            "time": self.time,
        }


@dataclass
class OccurrenceData:
    """One placement: instance name, owner-relative component file, 16-cell transform."""

    name: str = ""
    component: str = ""
    transform: Optional[List[float]] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"name": self.name, "component": self.component}
        if self.transform:
            body["transform"] = list(self.transform)
        return body


@dataclass
class PartRecipe:
    """The ``model:`` body of a part document. Empty sections are omitted."""

    units: Units = field(default_factory=Units)
    parameters: List[Any] = field(default_factory=list)
    work_features: List[Any] = field(default_factory=list)
    sketches: List[Any] = field(default_factory=list)
    features: List[Any] = field(default_factory=list)

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"units": self.units.to_yaml()}
        if self.parameters:
            body["parameters"] = [p.to_yaml() for p in self.parameters]
        if self.work_features:
            body["workFeatures"] = [w.to_yaml() for w in self.work_features]
        if self.sketches:
            body["sketches"] = [s.to_yaml() for s in self.sketches]
        if self.features:
            body["features"] = [f.to_yaml() for f in self.features]
        return body


@dataclass
class AssemblyRecipe:
    """The ``model:`` body of an assembly document: units plus placed occurrences."""

    units: Units = field(default_factory=Units)
    occurrences: List[OccurrenceData] = field(default_factory=list)

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"units": self.units.to_yaml()}
        if self.occurrences:
            body["occurrences"] = [o.to_yaml() for o in self.occurrences]
        return body


@dataclass
class OblikovatiDocument:
    """The serialized root of a .opd / .oad document."""

    schema_version: int = 2
    document_type: int = 1  # 1 = part (.opd), 2 = assembly (.oad)
    display_name: str = ""
    model: Optional[Any] = None  # PartRecipe or AssemblyRecipe

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {
            "schemaVersion": self.schema_version,
            "documentType": self.document_type,
            "displayName": self.display_name,
        }
        if self.model is not None:
            body["model"] = self.model.to_yaml()
        return body
