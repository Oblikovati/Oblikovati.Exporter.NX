// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Linq;
using Oblikovati.Exporter.NX.Fixtures;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class PatternTranslatorTests
    {
        private static PartRecipe Translate(NxDocument part) =>
            (PartRecipe)new DocumentTranslator().Translate(part, new ExportReport()).Model!;

        [Fact]
        public void TranslatesRectangularPatternWithRemappedSource()
        {
            FeatureData feature = Translate(NxSampleParts.RectPatternPart()).Features.Last();

            Assert.Equal("rectangular-pattern", feature.Kind);
            RectPatternData rp = Assert.IsType<RectPatternData>(feature.RectangularPattern);
            Assert.Equal(new[] { 0 }, rp.Source); // the extrude is recipe feature 0
            Assert.Equal(3, rp.CountX);
            Assert.Equal(new[] { 6.0, 0.0, 0.0 }, rp.StepX); // 60 mm -> 6 cm
        }

        [Fact]
        public void TranslatesCircularPatternFullTurn()
        {
            FeatureData feature = Translate(NxSampleParts.CircularPatternPart()).Features.Last();

            CircPatternData cp = Assert.IsType<CircPatternData>(feature.CircularPattern);
            Assert.Equal(4, cp.Count);
            Assert.Equal(2 * Math.PI, cp.Angle, 6); // 0 deg -> full revolution
            Assert.Equal(new[] { 0.0, 0.0, 1.0 }, cp.AxisDir);
        }

        [Fact]
        public void TranslatesMirrorFromOriginAndNormal()
        {
            FeatureData feature = Translate(NxSampleParts.MirrorPart()).Features.Last();

            MirrorData m = Assert.IsType<MirrorData>(feature.Mirror);
            Assert.Equal(new[] { 0 }, m.Source);
            Assert.Equal(new[] { 1.0, 0.0, 0.0 }, m.Normal);
        }

        [Fact]
        public void SkipsPatternWhoseSourceWasSkipped()
        {
            var part = new NxDocument { DisplayName = "p", Kind = NxDocumentKind.Part };
            part.Features.Add(new UnknownFeature());      // index 0, skipped
            var pattern = new NxRectangularPattern { CountX = 2, CountY = 1 };
            pattern.SourceFeatureIndices.Add(0);          // references the skipped feature
            part.Features.Add(pattern);
            var report = new ExportReport();

            var recipe = (PartRecipe)new DocumentTranslator().Translate(part, report).Model!;

            Assert.Empty(recipe.Features);
            Assert.True(report.HasWarnings);
        }

        private sealed class UnknownFeature : NxFeature
        {
        }
    }
}
