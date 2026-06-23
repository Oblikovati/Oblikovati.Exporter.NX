# SPDX-License-Identifier: GPL-2.0-only
"""Accumulates what the translator could and could not carry across.

Unsupported NX features are recorded here (never silently dropped, never STEP-
substituted) and surfaced to the user after export.
"""
from __future__ import annotations

from typing import List


class ExportReport:
    def __init__(self) -> None:
        self._warnings: List[str] = []

    @property
    def warnings(self) -> List[str]:
        return list(self._warnings)

    @property
    def has_warnings(self) -> bool:
        return len(self._warnings) > 0

    def unsupported(self, nx_type: str, feature_name: str) -> None:
        """Records that a feature of an NX type has no translation yet."""
        self._warnings.append(
            f"unsupported NX feature '{feature_name}' of type '{nx_type}' was skipped"
        )

    def warn(self, message: str) -> None:
        self._warnings.append(message)
