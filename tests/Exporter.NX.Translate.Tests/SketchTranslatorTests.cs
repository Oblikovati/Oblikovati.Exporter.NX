// SPDX-License-Identifier: GPL-2.0-only
using System.Linq;
using Oblikovati.Exporter.NX.Fixtures;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class SketchTranslatorTests
    {
        private static SketchData TranslateOnly(NxDocument part)
        {
            var doc = new DocumentTranslator().Translate(part, new ExportReport());
            return ((PartRecipe)doc.Model!).Sketches.Single();
        }

        [Fact]
        public void KeepsDistinctEndpointsWithCoincidentConstraints()
        {
            SketchData sketch = TranslateOnly(NxSampleParts.RectanglePart());

            // Engine format: each line keeps its own 2 endpoints (8 points) and corners
            // are joined by coincident constraints, not shared ids.
            Assert.Equal(8, sketch.Points.Count);
            Assert.Equal(4, sketch.Entities.Count);
            Assert.All(sketch.Entities, e => Assert.Equal("line", e.Kind));
            Assert.Equal(4, sketch.Constraints.Count(c => c.Kind == "coincident"));
        }

        [Fact]
        public void ConvertsMillimetresToCentimetres()
        {
            SketchData sketch = TranslateOnly(NxSampleParts.RectanglePart());

            // The 40x30 mm rectangle becomes 4x3 cm in database units.
            Assert.Contains(sketch.Points, p => p.X == 4.0 && p.Y == 0.0);
            Assert.Contains(sketch.Points, p => p.X == 4.0 && p.Y == 3.0);
        }

        [Fact]
        public void KeepsGeometricConstraints()
        {
            SketchData sketch = TranslateOnly(NxSampleParts.RectanglePart());

            Assert.Equal(2, sketch.Constraints.Count(c => c.Kind == "horizontal"));
            Assert.Equal(2, sketch.Constraints.Count(c => c.Kind == "vertical"));
            Assert.Single(sketch.Constraints, c => c.Kind == "fix");
        }

        [Fact]
        public void HorizontalConstraintTargetsLineEndpoints()
        {
            SketchData sketch = TranslateOnly(NxSampleParts.RectanglePart());

            ConstraintData horizontal = sketch.Constraints.First(c => c.Kind == "horizontal");
            Assert.Equal(2, horizontal.Points.Count);
            Assert.Empty(horizontal.Curves);
        }

        [Fact]
        public void DimensionsCarryParameterExpressions()
        {
            SketchData sketch = TranslateOnly(NxSampleParts.RectanglePart());

            Assert.Contains(sketch.Dimensions, d => d.Kind == "distance" && d.Expression == "width");
            Assert.Contains(sketch.Dimensions, d => d.Kind == "distance" && d.Expression == "height");
        }

        [Fact]
        public void CircleEmitsRadiusAndDiameterDimension()
        {
            SketchData sketch = TranslateOnly(NxSampleParts.CirclePart());

            EntityData circle = Assert.Single(sketch.Entities);
            Assert.Equal("circle", circle.Kind);
            Assert.Equal(2.0, circle.Radius); // 20 mm -> 2 cm
            Assert.Contains(sketch.Dimensions, d => d.Kind == "diameter" && d.Expression == "dia");
        }

        [Fact]
        public void PointsAndEntitiesShareOneIdSpace()
        {
            SketchData sketch = TranslateOnly(NxSampleParts.RectanglePart());

            var ids = sketch.Points.Select(p => p.Id)
                .Concat(sketch.Entities.Select(e => e.Id))
                .ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }
}
