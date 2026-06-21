// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Fixtures;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class WorkPlaneTranslatorTests
    {
        [Fact]
        public void TranslatesDatumPlaneToFixedFrame()
        {
            var part = (PartRecipe)new DocumentTranslator()
                .Translate(NxSampleParts.DatumPlanePart(), new ExportReport()).Model!;

            WorkFeatureData plane = Assert.Single(part.WorkFeatures);
            Assert.Equal("plane", plane.Collection);
            Assert.Equal("fixed-frame", plane.Kind);
            Assert.Equal(new[] { 0.0, 0.0, 1.0 }, plane.Position); // 10 mm -> 1 cm
            Assert.Equal(new[] { 1.0, 0.0, 0.0 }, plane.XAxis);
            Assert.Equal(new[] { 0.0, 1.0, 0.0 }, plane.YAxis);
        }
    }
}
