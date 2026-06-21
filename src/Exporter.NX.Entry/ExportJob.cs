// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Nx;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;

namespace Oblikovati.Exporter.NX.Entry
{
    /// <summary>
    /// The whole export, end to end and free of NXOpen: read the work document, translate
    /// the tree, write every file to <paramref name="sink"/>, and return the user summary.
    /// The NX entry point supplies a live session and a directory sink; tests supply fakes.
    /// </summary>
    public static class ExportJob
    {
        public static string Run(INxSession session, IDocumentSink sink)
        {
            var runner = new ExportRunner(session, new DocumentTranslator(), new RecipeYamlWriter());
            ExportOutput output = runner.Run();
            foreach (ExportFile file in output.Files)
            {
                sink.Write(file.FileName, file.Yaml);
            }

            return ExportReportFormatter.Summarize(output);
        }
    }
}
