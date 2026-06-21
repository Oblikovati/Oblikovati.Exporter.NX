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

        /// <summary>
        /// The fully-constrained rectangle extruded 50 mm into a 40x30x50 mm box (volume
        /// 60 cm^3) — exercises the sketch -> solid pipeline end to end.
        /// </summary>
        public static NxDocument BoxPart()
        {
            NxDocument part = RectanglePart();
            part.DisplayName = "box-part";
            part.Features.Add(new NxExtrude
            {
                Name = "Extrude1",
                SketchIndex = 0,
                ProfileIndex = 0,
                Operation = NxOperation.NewBody,
                Direction = NxExtentDirection.Positive,
                Distance = 50,
            });
            return part;
        }

        /// <summary>
        /// An offset square (x in [20,40], y in [0,20] mm) revolved full about a vertical
        /// centerline on the Y axis — a washer of 24*pi cm^3 (outer r 4 cm, inner r 2 cm,
        /// height 2 cm). Exercises revolve about the sketch's own centerline.
        /// </summary>
        public static NxDocument RevolvePart()
        {
            var part = new NxDocument
            {
                DisplayName = "revolve-part",
                Kind = NxDocumentKind.Part,
                LengthUnit = "mm",
            };
            part.Expressions.Add(new NxExpression { Name = "side", Formula = "20", Unit = "mm" });

            var sketch = new NxSketch { Name = "Section" };
            const long l0 = 1, l1 = 2, l2 = 3, l3 = 4, axis = 5;
            sketch.Curves.Add(Line(l0, 20, 0, 40, 0));   // bottom
            sketch.Curves.Add(Line(l1, 40, 0, 40, 20));  // outer
            sketch.Curves.Add(Line(l2, 40, 20, 20, 20)); // top
            sketch.Curves.Add(Line(l3, 20, 20, 20, 0));  // inner
            NxCurve centerline = Line(axis, 0, 0, 0, 20);
            centerline.Centerline = true;
            sketch.Curves.Add(centerline);

            Coincide(sketch, l0, NxCurvePointRole.End, l1, NxCurvePointRole.Start);
            Coincide(sketch, l1, NxCurvePointRole.End, l2, NxCurvePointRole.Start);
            Coincide(sketch, l2, NxCurvePointRole.End, l3, NxCurvePointRole.Start);
            Coincide(sketch, l3, NxCurvePointRole.End, l0, NxCurvePointRole.Start);

            sketch.Constraints.Add(OnCurves(NxConstraintKind.Horizontal, l0));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Horizontal, l2));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Vertical, l1));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Vertical, l3));
            sketch.Constraints.Add(Fix(l0, NxCurvePointRole.Start));
            sketch.Dimensions.Add(Distance(l0, NxCurvePointRole.Start, l0, NxCurvePointRole.End, "side"));
            sketch.Dimensions.Add(Distance(l3, NxCurvePointRole.Start, l3, NxCurvePointRole.End, "side"));

            // Pin the centerline (vertical on the Y axis, length "side").
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Vertical, axis));
            sketch.Constraints.Add(Fix(axis, NxCurvePointRole.Start));
            sketch.Dimensions.Add(Distance(axis, NxCurvePointRole.Start, axis, NxCurvePointRole.End, "side"));

            part.Sketches.Add(sketch);
            part.Features.Add(new NxRevolve
            {
                Name = "Revolve1",
                SketchIndex = 0,
                ProfileIndex = 0,
                Operation = NxOperation.NewBody,
                AngleDegrees = 0, // full revolution
            });
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

        /// <summary>Box (40x30 mm, extruded 50 mm = 60 cm^3) replicated 1x3 along +X (180 cm^3).</summary>
        public static NxDocument RectPatternPart()
        {
            NxDocument part = MakeBox("rect-pattern-part", 0);
            var pattern = new NxRectangularPattern
            {
                Name = "Pattern1",
                CountX = 3,
                CountY = 1,
                StepX = new double[] { 60, 0, 0 },
                StepY = new double[] { 0, 0, 0 },
            };
            pattern.SourceFeatureIndices.Add(0); // the extrude
            part.Features.Add(pattern);
            return part;
        }

        /// <summary>Box mirrored across the YZ plane (x = 0): source + reflection = 120 cm^3.</summary>
        public static NxDocument MirrorPart()
        {
            NxDocument part = MakeBox("mirror-part", 0);
            var mirror = new NxMirror
            {
                Name = "Mirror1",
                PlaneOrigin = new double[] { 0, 0, 0 },
                PlaneNormal = new double[] { 1, 0, 0 },
            };
            mirror.SourceFeatureIndices.Add(0);
            part.Features.Add(mirror);
            return part;
        }

        /// <summary>Box offset 100 mm from the Z axis, circular-patterned 4x full turn (240 cm^3).</summary>
        public static NxDocument CircularPatternPart()
        {
            NxDocument part = MakeBox("circular-pattern-part", 100);
            var pattern = new NxCircularPattern
            {
                Name = "Pattern1",
                Count = 4,
                AngleDegrees = 0, // full revolution
                AxisPoint = new double[] { 0, 0, 0 },
                AxisDir = new double[] { 0, 0, 1 },
            };
            pattern.SourceFeatureIndices.Add(0);
            part.Features.Add(pattern);
            return part;
        }

        /// <summary>
        /// A fully-constrained 40x30 mm rectangle whose lower-left corner sits at
        /// (x0, 0) mm, extruded 50 mm. The shared builder behind the pattern/mirror boxes.
        /// </summary>
        private static NxDocument MakeBox(string name, double x0)
        {
            var part = new NxDocument { DisplayName = name, Kind = NxDocumentKind.Part, LengthUnit = "mm" };
            part.Expressions.Add(new NxExpression { Name = "bw", Formula = "40", Unit = "mm" });
            part.Expressions.Add(new NxExpression { Name = "bh", Formula = "30", Unit = "mm" });

            var sketch = new NxSketch { Name = "Base" };
            const long l0 = 1, l1 = 2, l2 = 3, l3 = 4;
            sketch.Curves.Add(Line(l0, x0, 0, x0 + 40, 0));
            sketch.Curves.Add(Line(l1, x0 + 40, 0, x0 + 40, 30));
            sketch.Curves.Add(Line(l2, x0 + 40, 30, x0, 30));
            sketch.Curves.Add(Line(l3, x0, 30, x0, 0));
            Coincide(sketch, l0, NxCurvePointRole.End, l1, NxCurvePointRole.Start);
            Coincide(sketch, l1, NxCurvePointRole.End, l2, NxCurvePointRole.Start);
            Coincide(sketch, l2, NxCurvePointRole.End, l3, NxCurvePointRole.Start);
            Coincide(sketch, l3, NxCurvePointRole.End, l0, NxCurvePointRole.Start);
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Horizontal, l0));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Horizontal, l2));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Vertical, l1));
            sketch.Constraints.Add(OnCurves(NxConstraintKind.Vertical, l3));
            sketch.Constraints.Add(Fix(l0, NxCurvePointRole.Start));
            sketch.Dimensions.Add(Distance(l0, NxCurvePointRole.Start, l0, NxCurvePointRole.End, "bw"));
            sketch.Dimensions.Add(Distance(l3, NxCurvePointRole.Start, l3, NxCurvePointRole.End, "bh"));
            part.Sketches.Add(sketch);

            part.Features.Add(new NxExtrude
            {
                Name = "Extrude1",
                SketchIndex = 0,
                ProfileIndex = 0,
                Operation = NxOperation.NewBody,
                Direction = NxExtentDirection.Positive,
                Distance = 50,
            });
            return part;
        }

        /// <summary>
        /// An assembly placing the same 60 cm^3 box twice (instances at the origin and at
        /// x = 100 mm). The shared component exports once; the .oad references it by name.
        /// </summary>
        public static NxDocument AssemblyDoc()
        {
            NxDocument box = MakeBox("box-component", 0);
            var assembly = new NxDocument
            {
                DisplayName = "assembly",
                Kind = NxDocumentKind.Assembly,
                LengthUnit = "mm",
            };
            assembly.Occurrences.Add(new NxOccurrence { Name = "box-component:1", Component = box });
            assembly.Occurrences.Add(new NxOccurrence
            {
                Name = "box-component:2",
                Component = box,
                Position = new double[] { 100, 0, 0 },
            });
            return assembly;
        }

        /// <summary>A part carrying one datum plane offset 10 mm above XY (a fixed frame).</summary>
        public static NxDocument DatumPlanePart()
        {
            var part = new NxDocument
            {
                DisplayName = "datum-plane-part",
                Kind = NxDocumentKind.Part,
                LengthUnit = "mm",
            };
            part.WorkPlanes.Add(new NxWorkPlane
            {
                Name = "Datum1",
                Origin = new double[] { 0, 0, 10 },
                XAxis = new double[] { 1, 0, 0 },
                YAxis = new double[] { 0, 1, 0 },
            });
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
