// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.IO;
using NXOpen;
using NXOpen.Assemblies;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Production <see cref="INxSession"/> backed by the live NXOpen session. This is
    /// the single class allowed to read NXOpen types; everything downstream consumes
    /// the NX-neutral <see cref="NxDocument"/>. Extraction grows milestone by milestone
    /// (expressions M2, sketches M3, features M4...). Never exercised in CI — tests use
    /// a fake INxSession instead.
    /// </summary>
    public sealed class NxSessionAdapter : INxSession
    {
        private readonly Session _session;

        public NxSessionAdapter()
            : this(Session.GetSession())
        {
        }

        // Constructor injection keeps the NXOpen session a parameter, per CLAUDE.md.
        public NxSessionAdapter(Session session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public NxDocument ExtractWorkDocument()
        {
            Part? work = _session.Parts.Work;
            if (work == null)
            {
                throw new InvalidOperationException("no work part is open in NX");
            }

            Component? root = work.ComponentAssembly.RootComponent;
            return root != null && root.GetChildren().Length > 0
                ? ExtractAssembly(work, root)
                : ExtractPart(work);
        }

        // Reads one part into the IR the translator consumes: parameters, sketches, features.
        private NxDocument ExtractPart(Part part)
        {
            var document = new NxDocument
            {
                DisplayName = part.Leaf,
                Kind = NxDocumentKind.Part,
                LengthUnit = LengthUnitOf(part),
                AngleUnit = "deg",
            };
            ExpressionExtractor.Extract(part, document);
            SketchExtractor.Extract(part, document);
            FeatureExtractor.Extract(part, document);
            return document;
        }

        // Reads an assembly's occurrence tree; each component's prototype extracts as a part.
        private NxDocument ExtractAssembly(Part part, Component root)
        {
            var document = new NxDocument
            {
                DisplayName = part.Leaf,
                Kind = NxDocumentKind.Assembly,
                LengthUnit = LengthUnitOf(part),
            };
            var components = new ComponentExtractor(ExtractPart);
            foreach (Component child in root.GetChildren())
            {
                document.Occurrences.Add(components.Occurrence(child));
            }

            return document;
        }

        private static string LengthUnitOf(Part part) => part.PartUnits == PartUnits.Inches ? "in" : "mm";

        /// <summary>
        /// The folder to write exported documents into: the work part's directory so a
        /// reopened assembly resolves its components, or the temp folder for an unsaved part.
        /// </summary>
        public string OutputDirectory()
        {
            Part? work = _session.Parts.Work;
            string fullPath = work?.FullPath ?? string.Empty;
            string directory = fullPath.Length == 0 ? string.Empty : Path.GetDirectoryName(fullPath) ?? string.Empty;
            return directory.Length == 0 ? Path.GetTempPath() : directory;
        }

        /// <summary>Shows the export summary in NX's listing window.</summary>
        public void ShowMessage(string text)
        {
            ListingWindow window = _session.ListingWindow;
            window.Open();
            window.WriteLine(text);
        }
    }
}
