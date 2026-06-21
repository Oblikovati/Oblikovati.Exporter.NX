// SPDX-License-Identifier: GPL-2.0-only
using System;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Nx;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;

namespace Oblikovati.Exporter.NX.Entry
{
    /// <summary>
    /// Orchestrates one export: read the NX work document, translate it, render YAML.
    /// Pure of file I/O and NXOpen (collaborators are injected) so it is unit-testable
    /// with a fake <see cref="INxSession"/>. The caller decides where to write the text.
    /// </summary>
    public sealed class ExportRunner
    {
        private readonly INxSession _session;
        private readonly DocumentTranslator _translator;
        private readonly IRecipeWriter _writer;

        public ExportRunner(INxSession session, DocumentTranslator translator, IRecipeWriter writer)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public ExportResult Run()
        {
            NxDocument document = _session.ExtractWorkDocument();
            var report = new ExportReport();
            OblikovatiDocument recipe = _translator.Translate(document, report);
            string yaml = _writer.Write(recipe);
            string fileName = document.DisplayName + ExtensionFor(document.Kind);
            return new ExportResult(fileName, yaml, report);
        }

        private static string ExtensionFor(NxDocumentKind kind)
        {
            return kind == NxDocumentKind.Assembly ? ".oad" : ".opd";
        }
    }

    /// <summary>The product of one export: the suggested file name, its YAML, and the report.</summary>
    public sealed class ExportResult
    {
        public ExportResult(string fileName, string yaml, ExportReport report)
        {
            FileName = fileName;
            Yaml = yaml;
            Report = report;
        }

        public string FileName { get; }

        public string Yaml { get; }

        public ExportReport Report { get; }
    }
}
