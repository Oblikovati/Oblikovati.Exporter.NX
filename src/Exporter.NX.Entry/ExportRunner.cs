// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Nx;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;

namespace Oblikovati.Exporter.NX.Entry
{
    /// <summary>
    /// Orchestrates one export: read the NX work document, translate the whole document
    /// tree, and render each resulting document to YAML. Pure of file I/O and NXOpen
    /// (collaborators are injected) so it is unit-testable with a fake <see cref="INxSession"/>.
    /// A part yields one file; an assembly yields its .oad plus a file per referenced
    /// component. The caller decides where to write the files.
    /// </summary>
    public sealed class ExportRunner
    {
        private readonly INxSession _session;
        private readonly DocumentExporter _exporter;
        private readonly IRecipeWriter _writer;

        public ExportRunner(INxSession session, DocumentTranslator translator, IRecipeWriter writer)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _exporter = new DocumentExporter(translator ?? throw new ArgumentNullException(nameof(translator)));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public ExportOutput Run()
        {
            NxDocument document = _session.ExtractWorkDocument();
            var report = new ExportReport();
            var files = new List<ExportFile>();
            foreach (TranslatedDocument translated in _exporter.Export(document, report))
            {
                files.Add(new ExportFile(translated.FileName, _writer.Write(translated.Document)));
            }

            return new ExportOutput(files, report);
        }
    }

    /// <summary>One rendered document: its file name and YAML text.</summary>
    public sealed class ExportFile
    {
        public ExportFile(string fileName, string yaml)
        {
            FileName = fileName;
            Yaml = yaml;
        }

        public string FileName { get; }

        public string Yaml { get; }
    }

    /// <summary>The product of one export: every rendered file plus the report.</summary>
    public sealed class ExportOutput
    {
        public ExportOutput(IReadOnlyList<ExportFile> files, ExportReport report)
        {
            Files = files;
            Report = report;
        }

        public IReadOnlyList<ExportFile> Files { get; }

        public ExportReport Report { get; }
    }
}
