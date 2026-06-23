# SPDX-License-Identifier: GPL-2.0-only
"""The whole export, end to end and free of NXOpen.

Read the work document, translate the tree, write every file to the sink, and return the
user summary. The NX journal supplies a live session and a directory sink; tests supply
fakes.
"""
from __future__ import annotations

from ..recipe.yaml_writer import RecipeYamlWriter
from ..translate.document_translator import DocumentTranslator
from .document_sink import DocumentSink
from .export_runner import ExportRunner
from . import report_formatter


def run(session, sink: DocumentSink) -> str:
    runner = ExportRunner(session, DocumentTranslator(), RecipeYamlWriter())
    output = runner.run()
    for file in output.files:
        sink.write(file.file_name, file.yaml_text)
    return report_formatter.summarize(output)
