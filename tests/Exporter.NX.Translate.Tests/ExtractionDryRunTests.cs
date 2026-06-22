// SPDX-License-Identifier: GPL-2.0-only
using System.IO;
using System.Linq;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Nx;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    /// <summary>
    /// Dry-runs the extraction adapter over a fake NX scene (FakeNxScene) — exercising the
    /// extraction LOGIC end to end without NX. Validates the produced IR and that it renders
    /// to a document the Oblikovati reader accepts (the YAML is also written to a temp file
    /// for a manual oblikovati-cli open).
    /// </summary>
    public sealed class ExtractionDryRunTests
    {
        private static NxDocument Extract() =>
            new NxSessionAdapter(FakeNxScene.SampleBracket()).ExtractWorkDocument();

        [Fact]
        public void ExtractsParametersUnitsAndKind()
        {
            NxDocument doc = Extract();
            Assert.Equal("sample-bracket", doc.DisplayName);
            Assert.Equal(NxDocumentKind.Part, doc.Kind);
            Assert.Equal("mm", doc.LengthUnit);

            Assert.Equal(2, doc.Expressions.Count);
            Assert.Contains(doc.Expressions, e => e.Name == "width" && e.Formula == "40" && e.Unit == "mm");
            Assert.Contains(doc.Expressions, e => e.Name == "height" && e.Formula == "30");
        }

        [Fact]
        public void ExtractsRectangleSketchWithInferredCoincidence()
        {
            NxSketch sketch = Assert.Single(Extract().Sketches);
            Assert.Equal(4, sketch.Curves.Count);
            Assert.All(sketch.Curves, c => Assert.Equal(NxCurveKind.Line, c.Kind));
            // Four corners meet → four inferred coincident constraints, so the profile closes.
            Assert.Equal(4, sketch.Constraints.Count(c => c.Kind == NxConstraintKind.Coincident));
        }

        [Fact]
        public void ExtractsExtrudeAndFillet()
        {
            NxDocument doc = Extract();
            Assert.Equal(2, doc.Features.Count);

            var extrude = Assert.IsType<NxExtrude>(doc.Features[0]);
            Assert.Equal(0, extrude.SketchIndex);
            Assert.Equal(50, extrude.Distance);

            var fillet = Assert.IsType<NxFillet>(doc.Features[1]);
            Assert.Equal(5, fillet.RadiusMm);
            NxEdgeDescriptor edge = Assert.Single(fillet.Edges);
            Assert.Equal(new[] { 20.0, 0.0, 50.0 }, edge.Midpoint); // mm, the top edge midpoint
        }

        [Fact]
        public void RendersToADocumentAndWritesDryRunArtifact()
        {
            var report = new ExportReport();
            OblikovatiDocument recipe = new DocumentTranslator().Translate(Extract(), report);
            string yaml = new RecipeYamlWriter().Write(recipe);

            Assert.Contains("kind: extrude", yaml);
            Assert.Contains("kind: fillet", yaml);
            Assert.Contains("geomEdges", yaml);

            // Leave an artifact for a manual `oblikovati-cli open` (a real-reader dry run).
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "nx-extraction-dryrun.opd"), yaml);
        }
    }
}
