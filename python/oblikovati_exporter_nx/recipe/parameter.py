# SPDX-License-Identifier: GPL-2.0-only
"""Parameter recipe row. Mirrors parameterRecipe in compdef/serialize.go."""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, Optional


@dataclass
class ParameterRecipe:
    """One parameter. Editable kinds carry an expression with units/formulas inline."""

    name: str = ""
    kind: str = "user"  # user | model | reference | derived | table
    expression: Optional[str] = None
    comment: Optional[str] = None

    def to_yaml(self) -> Dict[str, Any]:
        body: Dict[str, Any] = {"name": self.name, "kind": self.kind}
        if self.expression is not None:
            body["expression"] = self.expression
        if self.comment is not None:
            body["comment"] = self.comment
        return body
