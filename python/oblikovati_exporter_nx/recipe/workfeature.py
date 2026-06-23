# SPDX-License-Identifier: GPL-2.0-only
"""Work-feature recipe row. Mirrors WorkFeatureData in feature/serialize_work.go."""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, List, Optional


@dataclass
class WorkFeatureData:
    """A datum feature. A fixed-frame plane is self-contained (origin + two axes)."""

    collection: str = "plane"
    kind: str = "fixed-frame"
    position: Optional[List[float]] = None
    xaxis: Optional[List[float]] = None
    yaxis: Optional[List[float]] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"collection": self.collection, "kind": self.kind}
        if self.position is not None:
            body["position"] = list(self.position)
        if self.xaxis is not None:
            body["xaxis"] = list(self.xaxis)
        if self.yaxis is not None:
            body["yaxis"] = list(self.yaxis)
        return body
