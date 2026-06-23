# SPDX-License-Identifier: GPL-2.0-only
"""Where exported document files are written.

Abstracted so the orchestration is testable with a fake sink instead of touching the
filesystem.
"""
from __future__ import annotations

import os
from typing import Protocol


class DocumentSink(Protocol):
    """A destination for rendered document files (an assembly's components share it)."""

    def write(self, file_name: str, yaml_text: str) -> None: ...


class DirectoryDocumentSink:
    """Writes document files into a directory on disk (UTF-8, LF preserved)."""

    def __init__(self, directory: str) -> None:
        self._directory = directory

    def write(self, file_name: str, yaml_text: str) -> None:
        path = os.path.join(self._directory, file_name)
        with open(path, "w", encoding="utf-8", newline="") as handle:
            handle.write(yaml_text)
