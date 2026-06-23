# SPDX-License-Identifier: GPL-2.0-only
"""NX Open Python journal: export the open document to native Oblikovati .opd/.oad.

This is the entry point NX runs — either played interactively (File ▸ Execute ▸ NX Open,
or Tools ▸ Journal ▸ Play) or bound to a ribbon/menu button (see deploy-python/). Being a
journal, it runs through NX's embedded Python interpreter with **no compiled, code-signed
DLL** — the reason this Python edition exists alongside the C# add-in.

The journal keeps no logic of its own: it constructs the live NXOpen-backed session
adapter and a directory sink, then delegates to the testable export job. The exporter
package is shipped next to this file, so its folder is added to ``sys.path`` first.
"""
import os
import sys

# The installable layout ships the oblikovati_exporter_nx/ package beside this journal.
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from oblikovati_exporter_nx.entry import export_job  # noqa: E402
from oblikovati_exporter_nx.entry.document_sink import DirectoryDocumentSink  # noqa: E402
from oblikovati_exporter_nx.nx.session_adapter import NxSessionAdapter  # noqa: E402


def main() -> None:
    adapter = NxSessionAdapter()
    sink = DirectoryDocumentSink(adapter.output_directory())
    summary = export_job.run(adapter, sink)
    adapter.show_message(summary)


# NX plays a journal as the top-level module, so run on import; also callable by name.
if __name__ == "__main__":
    main()
