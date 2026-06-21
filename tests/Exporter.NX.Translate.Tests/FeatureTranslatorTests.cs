// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Fixtures;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class FeatureTranslatorTests
    {
        private static PartRecipe Translate(NxDocument part) =>
            (PartRecipe)new DocumentTranslator().Translate(part, new ExportReport()).Model!;

        [Fact]
        public void TranslatesExtrudeReferencingSketchAndProfile()
        {
            FeatureData feature = Assert.Single(Translate(NxSampleParts.BoxPart()).Features);

            Assert.Equal("extrude", feature.Kind);
            ExtrudeData ex = Assert.IsType<ExtrudeData>(feature.Extrude);
            Assert.Equal(0, ex.Sketch);
            Assert.Equal(new[] { 0 }, ex.Profiles);
            Assert.Equal("newBody", ex.Operation);
            Assert.Equal("distance", ex.Extent);
            Assert.Equal("positive", ex.Direction);
            Assert.Equal(5.0, ex.Distance); // 50 mm -> 5 cm
        }

        [Fact]
        public void TranslatesRevolveInOwnCenterlineMode()
        {
            FeatureData feature = Assert.Single(Translate(NxSampleParts.RevolvePart()).Features);

            Assert.Equal("revolve", feature.Kind);
            RevolveData rev = Assert.IsType<RevolveData>(feature.Revolve);
            Assert.Equal(0, rev.Sketch);
            Assert.Equal(0, rev.Profile);
            Assert.Equal("newBody", rev.Operation);
            Assert.Null(rev.Angle); // 0 degrees -> full revolution, left unset
        }

        [Fact]
        public void MarksCenterlineEntity()
        {
            var part = (PartRecipe)new DocumentTranslator()
                .Translate(NxSampleParts.RevolvePart(), new ExportReport()).Model!;
            Assert.Contains(part.Sketches[0].Entities, e => e.Centerline == true);
        }

        [Fact]
        public void MapsBooleanOperations()
        {
            var part = new NxDocument { DisplayName = "p", Kind = NxDocumentKind.Part };
            part.Features.Add(new NxExtrude { SketchIndex = 0, Operation = NxOperation.Cut, Distance = 10 });

            ExtrudeData ex = Translate(part).Features[0].Extrude!;
            Assert.Equal("cut", ex.Operation);
        }

        [Fact]
        public void RecordsUnsupportedFeatureInReport()
        {
            var part = new NxDocument { DisplayName = "p", Kind = NxDocumentKind.Part };
            part.Features.Add(new UnknownFeature { Name = "Mystery" });
            var report = new ExportReport();

            new DocumentTranslator().Translate(part, report);

            Assert.True(report.HasWarnings);
            Assert.Empty(((PartRecipe)new DocumentTranslator().Translate(part, new ExportReport()).Model!).Features);
        }

        // A feature kind the translator does not handle yet.
        private sealed class UnknownFeature : NxFeature
        {
        }
    }
}
