// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Fixtures
{
    /// <summary>
    /// NX-neutral sample documents used both by the unit tests and by the CI round-trip
    /// generator, so the exact inputs that are asserted are also opened by the real
    /// Oblikovati reader. Coordinates are in millimetres (the IR contract).
    /// </summary>
    public static class NxSampleParts
    {
        public static NxDocument EmptyPart() =>
            new NxDocument { DisplayName = "empty-part", Kind = NxDocumentKind.Part };

        public static NxDocument ParametricPart()
        {
            var part = new NxDocument
            {
                DisplayName = "parametric-part",
                Kind = NxDocumentKind.Part,
                LengthUnit = "mm",
            };
            part.Expressions.Add(new NxExpression { Name = "width", Formula = "40", Unit = "mm" });
            part.Expressions.Add(new NxExpression { Name = "twice", Formula = "width * 2", Unit = "mm" });
            return part;
        }

        /// <summary>
        /// A 40x30 mm rectangle, fully constrained (DOF 0): four coincident-cornered
        /// lines, horizontal/vertical edges, a fixed origin corner, and width/height
        /// dimensions driven by parameters.
        /// </summary>
        public static NxDocument RectanglePart()
        {
            var part = new NxDocument
            {
                DisplayName = "rectangle-part",
                Kind = NxDocumentKind.Part,
                LengthUnit = "mm",
            };
            part.Expressions.Add(new NxExpression { Name = "width", Formula = "40", Unit = "mm" });
            part.Expressions.Add(new NxExpression { Name = "height", Formula = "30", Unit = "mm" });

            var sketch = new NxSketch { Name = "Rectangle" };
            const long l0 = 1, l1 = 2, l2 = 3, l3 = 4;
            sketch.Curves.Add(Line(l0, 0, 0, 40, 0));   // bottom
            sketch.Curves.Add(Line(l1, 40, 0, 40, 30)); // right
            sketch.Curves.Add(Line(l2, 40, 30, 0, 30)); // top
            sketch.Curves.Add(Line(l3, 0, 30, 0, 0));   // left

            Coincide(sketch, l0, NxCurvePointRole.End, l1, NxCurvePointRole.Start);
            Coincide(sketch, l1, NxCurvePointRole.End, l2, NxCurvePointRole.Start);
            Coincide(sketch, l2, NxCurvePointRole.End, l3, NxCurvePointRole.Start);
            Coincide(sketch, l3, NxCurvePointRole.End, l0, NxCurvePointRole.Start);

            sketch.Constraints.Add(OnCurves(NxConstraintKind.Horizontal, l0));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Horizontal, l2));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Vertical, l1));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Vertical, l3));
            sketch.Constraints.Add(Fix(l0, NxCurvePointRole.Start));

            sketch.Dimensions.Add(Distance(l0, NxCurvePointRole.Start, l0, NxCurvePointRole.End, "width"));
            sketch.Dimensions.Add(Distance(l3, NxCurvePointRole.Start, l3, NxCurvePointRole.End, "height"));

            part.Sketches.Add(sketch);
            return part;
        }

        /// <summary>A circle fixed at the origin with a diameter dimension (DOF 0).</summary>
        public static NxDocument CirclePart()
        {
            var part = new NxDocument
            {
                DisplayName = "circle-part",
                Kind = NxDocumentKind.Part,
                LengthUnit = "mm",
            };
            part.Expressions.Add(new NxExpression { Name = "dia", Formula = "40", Unit = "mm" });

            var sketch = new NxSketch { Name = "Circle" };
            const long c0 = 1;
            sketch.Curves.Add(new NxCurve { Id = c0, Kind = NxCurveKind.Circle, Center = new double[] { 0, 0 }, Radius = 20 });
            sketch.Constraints.Add(Fix(c0, NxCurvePointRole.Center));
            sketch.Dimensions.Add(new NxSketchDimension { Kind = NxDimensionKind.Diameter, Expression = "dia" });
            sketch.Dimensions[0].Curves.Add(c0);

            part.Sketches.Add(sketch);
            return part;
        }

        private static NxCurve Line(long id, double x0, double y0, double x1, double y1) =>
            new NxCurve
            {
                Id = id,
                Kind = NxCurveKind.Line,
                Start = new[] { x0, y0 },
                End = new[] { x1, y1 },
            };

        private static void Coincide(NxSketch s, long ca, NxCurvePointRole ra, long cb, NxCurvePointRole rb)
        {
            var c = new NxSketchConstraint { Kind = NxConstraintKind.Coincident };
            c.Points.Add(new NxPointRef(ca, ra));
            c.Points.Add(new NxPointRef(cb, rb));
            s.Constraints.Add(c);
        }

        private static NxSketchConstraint OnCurves(NxConstraintKind kind, params long[] curves)
        {
            var c = new NxSketchConstraint { Kind = kind };
            foreach (long id in curves) c.Curves.Add(id);
            return c;
        }

        private static NxSketchConstraint Fix(long curve, NxCurvePointRole role)
        {
            var c = new NxSketchConstraint { Kind = NxConstraintKind.Fix };
            c.Points.Add(new NxPointRef(curve, role));
            return c;
        }

        private static NxSketchDimension Distance(long ca, NxCurvePointRole ra, long cb, NxCurvePointRole rb, string expr)
        {
            var d = new NxSketchDimension { Kind = NxDimensionKind.Distance, Expression = expr };
            d.Points.Add(new NxPointRef(ca, ra));
            d.Points.Add(new NxPointRef(cb, rb));
            return d;
        }
    }
}
