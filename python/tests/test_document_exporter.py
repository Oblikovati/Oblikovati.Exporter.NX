# SPDX-License-Identifier: GPL-2.0-only
"""Tests for the document tree walk: part vs assembly, dedup, unique file names."""
from oblikovati_exporter_nx.fixtures import sample_parts
from oblikovati_exporter_nx.model.document import NxDocument, NxDocumentKind, NxOccurrence
from oblikovati_exporter_nx.translate.document_exporter import DocumentExporter
from oblikovati_exporter_nx.translate.document_translator import DocumentTranslator
from oblikovati_exporter_nx.translate.report import ExportReport


def _export(root):
    return DocumentExporter(DocumentTranslator()).export(root, ExportReport())


def test_part_exports_one_opd():
    files = _export(sample_parts.box_part())
    assert [f.file_name for f in files] == ["box-part.opd"]
    assert files[0].document.document_type == int(NxDocumentKind.PART)


def test_assembly_exports_oad_plus_shared_component_once():
    files = _export(sample_parts.assembly_doc())
    names = sorted(f.file_name for f in files)
    assert names == ["assembly.oad", "box-component.opd"]
    oad = next(f for f in files if f.file_name == "assembly.oad").document
    # Both occurrences reference the single shared component file.
    components = [o.component for o in oad.model.occurrences]
    assert components == ["box-component.opd", "box-component.opd"]


def test_duplicate_display_names_get_unique_file_names():
    a = NxDocument(display_name="part", kind=NxDocumentKind.PART)
    b = NxDocument(display_name="part", kind=NxDocumentKind.PART)
    asm = NxDocument(display_name="asm", kind=NxDocumentKind.ASSEMBLY)
    asm.occurrences.append(NxOccurrence(name="a", component=a))
    asm.occurrences.append(NxOccurrence(name="b", component=b))
    names = sorted(f.file_name for f in _export(asm))
    assert names == ["asm.oad", "part.opd", "part_2.opd"]


def test_untitled_fallback_for_empty_display_name():
    files = _export(NxDocument(display_name="", kind=NxDocumentKind.PART))
    assert files[0].file_name == "untitled.opd"
