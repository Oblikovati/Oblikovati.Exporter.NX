# SPDX-License-Identifier: GPL-2.0-only
"""End-to-end orchestration test driven by a fake NX session and a fake sink."""
from oblikovati_exporter_nx.entry import export_job, report_formatter
from oblikovati_exporter_nx.entry.export_runner import ExportRunner
from oblikovati_exporter_nx.fixtures import sample_parts
from oblikovati_exporter_nx.recipe.yaml_writer import RecipeYamlWriter
from oblikovati_exporter_nx.translate.document_translator import DocumentTranslator


class FakeNxSession:
    """A fake NxSession that yields a prepared NX-neutral document (no NXOpen)."""

    def __init__(self, document):
        self._document = document

    def extract_work_document(self):
        return self._document


class FakeDocumentSink:
    """Collects written files in memory instead of touching the filesystem."""

    def __init__(self):
        self.written = {}

    def write(self, file_name, yaml_text):
        self.written[file_name] = yaml_text


def test_export_job_writes_part_and_returns_summary():
    sink = FakeDocumentSink()
    summary = export_job.run(FakeNxSession(sample_parts.box_part()), sink)
    assert "box-part.opd" in sink.written
    assert sink.written["box-part.opd"].startswith("schemaVersion: 2\n")
    assert "Exported 1 document(s)" in summary
    assert "No warnings" in summary


def test_export_job_writes_assembly_and_components():
    sink = FakeDocumentSink()
    export_job.run(FakeNxSession(sample_parts.assembly_doc()), sink)
    assert set(sink.written) == {"assembly.oad", "box-component.opd"}


def test_runner_collects_warnings_in_report():
    from oblikovati_exporter_nx.model.document import NxDocument, NxDocumentKind
    from oblikovati_exporter_nx.model.feature import NxFeature

    doc = NxDocument(display_name="p", kind=NxDocumentKind.PART)
    doc.features.append(NxFeature(name="weird"))  # base feature => unsupported
    output = ExportRunner(FakeNxSession(doc), DocumentTranslator(), RecipeYamlWriter()).run()
    assert output.report.has_warnings
    summary = report_formatter.summarize(output)
    assert "need attention" in summary
