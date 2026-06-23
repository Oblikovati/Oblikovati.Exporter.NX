# SPDX-License-Identifier: GPL-2.0-only
"""The thin seam over a live NX session (NXOpen-free so tests can fake it).

The orchestrator depends on this Protocol, not on NXOpen, so it can be driven by a fake
in tests. The production implementation is ``NxSessionAdapter`` in session_adapter.py,
which is the only class allowed to read NXOpen types.
"""
from __future__ import annotations

from typing import Protocol

from ..model.document import NxDocument


class NxSession(Protocol):
    """Reads the current work part/assembly into the NX-neutral IR."""

    def extract_work_document(self) -> NxDocument: ...
