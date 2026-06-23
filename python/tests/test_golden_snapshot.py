# SPDX-License-Identifier: GPL-2.0-only
"""Snapshot test: the Python exporter must reproduce the proven golden documents.

The committed goldens under ``goldens/`` are the byte-for-byte output the C# add-in
produces and round-trips through the real Oblikovati reader (CI Job 2). Asserting the
Python emitter matches them keeps the two exporters in exact lockstep and guards against
schema/format drift in either direction.
"""
import os

import pytest

from oblikovati_exporter_nx.fixtures import sample_parts
from oblikovati_exporter_nx.recipe.yaml_writer import RecipeYamlWriter
from oblikovati_exporter_nx.translate.document_exporter import DocumentExporter
from oblikovati_exporter_nx.translate.document_translator import DocumentTranslator
from oblikovati_exporter_nx.translate.report import ExportReport

_GOLDENS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "goldens")


def _export_all():
    exporter = DocumentExporter(DocumentTranslator())
    writer = RecipeYamlWriter()
    produced = {}
    for fixture in sample_parts.all_fixtures():
        for translated in exporter.export(fixture, ExportReport()):
            produced[translated.file_name] = writer.write(translated.document)
    return produced


_PRODUCED = _export_all()


@pytest.mark.parametrize("file_name", sorted(_PRODUCED))
def test_matches_committed_golden(file_name):
    golden_path = os.path.join(_GOLDENS_DIR, file_name)
    with open(golden_path, "r", encoding="utf-8", newline="") as handle:
        expected = handle.read()
    assert _PRODUCED[file_name] == expected


def test_every_golden_is_produced():
    on_disk = {f for f in os.listdir(_GOLDENS_DIR) if f.endswith((".opd", ".oad"))}
    assert on_disk == set(_PRODUCED)
