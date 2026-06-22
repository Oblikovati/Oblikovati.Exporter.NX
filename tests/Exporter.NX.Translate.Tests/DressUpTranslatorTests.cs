// SPDX-License-Identifier: GPL-2.0-only
using System.Linq;
using Oblikovati.Exporter.NX.Fixtures;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class DressUpTranslatorTests
    {
        private static PartRecipe Translate(Model.NxDocument part) =>
            (PartRecipe)new DocumentTranslator().Translate(part, new ExportReport()).Model!;

        [Fact]
        public void TranslatesFilletWithGeometricEdge()
        {
            FeatureData feature = Translate(NxSampleParts.FilletedBoxPart()).Features.Last();

            Assert.Equal("fillet", feature.Kind);
            EdgeDressData fil = Assert.IsType<EdgeDressData>(feature.Fillet);
            Assert.Equal(0.5, fil.Value); // 5 mm -> 0.5 cm
            GeomEdgeRefData e = Assert.Single(fil.GeomEdges);
            Assert.Equal(new[] { 2.0, 0.0, 5.0 }, e.Midpoint); // 20,0,50 mm -> cm
            Assert.Equal(new[] { 1.0, 0.0, 0.0 }, e.Direction);
        }

        [Fact]
        public void TranslatesChamferWithGeometricEdge()
        {
            FeatureData feature = Translate(NxSampleParts.ChamferedBoxPart()).Features.Last();
            Assert.Equal("chamfer", feature.Kind);
            EdgeDressData ch = Assert.IsType<EdgeDressData>(feature.Chamfer);
            Assert.Equal(0.5, ch.Value);
            Assert.Single(ch.GeomEdges);
        }

        [Fact]
        public void TranslatesShellWithGeometricFace()
        {
            FeatureData feature = Translate(NxSampleParts.ShelledBoxPart()).Features.Last();
            Assert.Equal("shell", feature.Kind);
            FaceDressData sh = Assert.IsType<FaceDressData>(feature.Shell);
            Assert.Equal(0.5, sh.Value);
            GeomFaceRefData f = Assert.Single(sh.GeomFaces);
            Assert.Equal(new[] { 2.0, 1.5, 5.0 }, f.Centroid);
        }

        [Fact]
        public void TranslatesHoleWithGeometricPlacementFace()
        {
            FeatureData feature = Translate(NxSampleParts.HoledBoxPart()).Features.Last();

            Assert.Equal("hole", feature.Kind);
            HoleData hole = Assert.IsType<HoleData>(feature.Hole);
            Assert.Equal(1.0, hole.Diameter); // 10 mm -> 1 cm
            Assert.Equal(2.0, hole.Depth);    // 20 mm -> 2 cm
            Assert.Equal("drilled", hole.Type);
            Assert.NotNull(hole.GeomFace);
            Assert.Equal(new[] { 2.0, 1.5, 5.0 }, hole.GeomFace!.Centroid); // 20,15,50 mm -> cm
            Assert.Equal(new[] { 0.0, 0.0, 1.0 }, hole.GeomFace!.Normal);
        }
    }
}
