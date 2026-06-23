# SPDX-License-Identifier: GPL-2.0-only
"""Hands out monotonically increasing document-local ids.

Sketches, points and entities share one id space (matching the Go codec, where a
sketch's id precedes its points' and entities' ids), so one allocator threads through
the document.
"""
from __future__ import annotations


class IdAllocator:
    def __init__(self, start: int = 1) -> None:
        self._next = start

    def next(self) -> int:
        value = self._next
        self._next += 1
        return value
