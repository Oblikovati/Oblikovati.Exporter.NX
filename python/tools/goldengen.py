# SPDX-License-Identifier: GPL-2.0-only
"""Writes golden documents from the shared fixtures to a directory.

Mirrors the C# tools/GoldenGen so CI Job 2 opens the exact inputs the unit tests assert,
with the real oblikovati-cli. Each fixture goes through the full document exporter, so an
assembly fixture emits its .oad plus its component files.

Usage: python tools/goldengen.py <output-dir>   (run with PYTHONPATH=python)
"""
from __future__ import annotations

import os
import sys

# Allow running as a loose script (PYTHONPATH unset) from the repo's python/ folder.
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from oblikovati_exporter_nx.fixtures import sample_parts  # noqa: E402
from oblikovati_exporter_nx.recipe.yaml_writer import RecipeYamlWriter  # noqa: E402
from oblikovati_exporter_nx.translate.document_exporter import DocumentExporter  # noqa: E402
from oblikovati_exporter_nx.translate.document_translator import DocumentTranslator  # noqa: E402
from oblikovati_exporter_nx.translate.report import ExportReport  # noqa: E402


def main(argv: list) -> int:
    if len(argv) != 2:
        sys.stderr.write("usage: goldengen.py <output-dir>\n")
        return 2

    out_dir = argv[1]
    os.makedirs(out_dir, exist_ok=True)

    exporter = DocumentExporter(DocumentTranslator())
    writer = RecipeYamlWriter()
    for fixture in sample_parts.all_fixtures():
        for translated in exporter.export(fixture, ExportReport()):
            path = os.path.join(out_dir, translated.file_name)
            with open(path, "w", encoding="utf-8", newline="") as handle:
                handle.write(writer.write(translated.document))
            print("wrote " + path)

    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
