# SPDX-License-Identifier: GPL-2.0-only
"""The directory sink writes files with LF preserved."""
import os

from oblikovati_exporter_nx.entry.document_sink import DirectoryDocumentSink


def test_directory_sink_writes_file_verbatim(tmp_path):
    sink = DirectoryDocumentSink(str(tmp_path))
    sink.write("doc.opd", "schemaVersion: 2\nfoo: bar\n")
    path = os.path.join(str(tmp_path), "doc.opd")
    with open(path, "r", encoding="utf-8", newline="") as handle:
        assert handle.read() == "schemaVersion: 2\nfoo: bar\n"
