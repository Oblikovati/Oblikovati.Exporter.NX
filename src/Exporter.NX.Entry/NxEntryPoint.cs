// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Nx;

namespace Oblikovati.Exporter.NX.Entry
{
    /// <summary>
    /// The method NX invokes from the ribbon button (wired in the .men deploy file). It is
    /// the only place that constructs the real NXOpen-backed adapter and a directory sink;
    /// all logic lives in the testable <see cref="ExportJob"/>.
    /// </summary>
    public static class NxEntryPoint
    {
        /// <summary>
        /// NX managed entry point. Exports the work document (and, for an assembly, its
        /// components) next to the source part, then shows the summary in the listing window.
        /// </summary>
        public static void Main()
        {
            var adapter = new NxSessionAdapter();
            var sink = new DirectoryDocumentSink(adapter.OutputDirectory());
            string summary = ExportJob.Run(adapter, sink);
            adapter.ShowMessage(summary);
        }

        /// <summary>Required by NX to unload the managed assembly cleanly.</summary>
        public static int GetUnloadOption(string dummy)
        {
            // 1 == Unload immediately after the callback returns.
            return 1;
        }
    }
}
