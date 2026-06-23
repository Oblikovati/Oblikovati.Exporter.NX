# SPDX-License-Identifier: GPL-2.0-only
"""Orchestrates one export: read the NX work document, translate the tree, render YAML.

Pure of file I/O and NXOpen (collaborators are injected) so it is unit-testable with a
fake session. A part yields one file; an assembly yields its .oad plus a file per
referenced component. The caller decides where to write the files.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import List

from ..recipe.yaml_writer import RecipeYamlWriter
from ..translate.document_exporter import DocumentExporter
from ..translate.document_translator import DocumentTranslator
from ..translate.report import ExportReport


@dataclass
class ExportFile:
    """One rendered document: its file name and YAML text."""

    file_name: str
    yaml_text: str


@dataclass
class ExportOutput:
    """The product of one export: every rendered file plus the report."""

    files: List[ExportFile]
    report: ExportReport


class ExportRunner:
    def __init__(self, session, translator: DocumentTranslator, writer: RecipeYamlWriter) -> None:
        self._session = session
        self._exporter = DocumentExporter(translator)
        self._writer = writer

    def run(self) -> ExportOutput:
        document = self._session.extract_work_document()
        report = ExportReport()
        files: List[ExportFile] = []
        for translated in self._exporter.export(document, report):
            files.append(ExportFile(translated.file_name, self._writer.write(translated.document)))
        return ExportOutput(files, report)
