// SPDX-License-Identifier: GPL-2.0-only
using NXOpen;
using NXOpen.Assemblies;
using NXOpen.Features;

namespace Oblikovati.Exporter.NX.Tests
{
    // Fake NXOpen objects: concrete subclasses of the (all-virtual) facade stubs that return
    // canned data instead of throwing. They let the real extraction adapter run over a sample
    // part with no NX — a dry run of the extraction LOGIC (not the real NXOpen binding).

    internal sealed class FakeUnit : Unit
    {
        private readonly string _symbol;
        public FakeUnit(string symbol) => _symbol = symbol;
        public override string Symbol => _symbol;
    }

    internal sealed class FakeExpression : Expression
    {
        private readonly string _name, _rhs, _type;
        private readonly double _value;
        private readonly Unit? _unit;
        private readonly NXOpen.Features.Feature? _owner;

        public FakeExpression(string name, string rhs, string type, double value, Unit? unit, NXOpen.Features.Feature? owner = null)
        {
            _name = name;
            _rhs = rhs;
            _type = type;
            _value = value;
            _unit = unit;
            _owner = owner;
        }

        public override string Name => _name;
        public override string RightHandSide => _rhs;
        public override string Type => _type;
        public override double Value => _value;
        public override Unit Units => _unit!;
        public override NXOpen.Features.Feature? GetOwningFeature() => _owner;
    }

    internal sealed class FakeExpressionCollection : ExpressionCollection
    {
        private readonly Expression[] _items;
        public FakeExpressionCollection(Expression[] items) => _items = items;
        public override Expression[] ToArray() => _items;
    }

    internal sealed class FakeLine : Line
    {
        private readonly Point3d _start, _end;
        public FakeLine(Point3d start, Point3d end)
        {
            _start = start;
            _end = end;
        }

        public override Point3d StartPoint => _start;
        public override Point3d EndPoint => _end;
    }

    internal sealed class FakeSketch : Sketch
    {
        private readonly string _name;
        private readonly NXObject[] _geometry;
        public FakeSketch(string name, NXObject[] geometry)
        {
            _name = name;
            _geometry = geometry;
        }

        public override string Name => _name;
        public override NXObject[] GetAllGeometry() => _geometry;
    }

    internal sealed class FakeSketchCollection : SketchCollection
    {
        private readonly Sketch[] _items;
        public FakeSketchCollection(Sketch[] items) => _items = items;
        public override Sketch[] ToArray() => _items;
    }

    internal sealed class FakeEdge : Edge
    {
        private readonly Point3d _v1, _v2;
        public FakeEdge(Point3d v1, Point3d v2)
        {
            _v1 = v1;
            _v2 = v2;
        }

        public override void GetVertices(out Point3d vertex1, out Point3d vertex2)
        {
            vertex1 = _v1;
            vertex2 = _v2;
        }
    }

    internal sealed class FakeScCollector : ScCollector
    {
        private readonly NXObject[] _objects;
        public FakeScCollector(NXObject[] objects) => _objects = objects;
        public override NXObject[] GetObjects() => _objects;
    }

    internal sealed class FakeSection : Section
    {
        private readonly NXObject[] _curves;
        public FakeSection(NXObject[] curves) => _curves = curves;
        public override NXObject[] GetOutputCurves() => _curves;
    }

    internal sealed class FakeLimit : Limit
    {
        private readonly Expression _value;
        public FakeLimit(double value) => _value = new FakeExpression("limit", value.ToString(System.Globalization.CultureInfo.InvariantCulture), "Number", value, null);
        public override Expression Value => _value;
    }

    internal sealed class FakeLimits : Limits
    {
        private readonly Limit _start, _end;
        public FakeLimits(double start, double end)
        {
            _start = new FakeLimit(start);
            _end = new FakeLimit(end);
        }

        public override Limit StartExtend => _start;
        public override Limit EndExtend => _end;
    }

    internal sealed class FakeExtrudeBuilder : ExtrudeBuilder
    {
        private readonly Section _section;
        private readonly Limits _limits;
        public FakeExtrudeBuilder(Section section, Limits limits)
        {
            _section = section;
            _limits = limits;
        }

        public override Section Section => _section;
        public override Limits Limits => _limits;
        public override void Destroy() { }
    }

    internal sealed class FakeEdgeBlendBuilder : EdgeBlendBuilder
    {
        private readonly ScCollector _edges;
        public FakeEdgeBlendBuilder(ScCollector edges) => _edges = edges;
        public override ScCollector Edges => _edges;
        public override void Destroy() { }
    }

    internal sealed class FakeFeature : NXOpen.Features.Feature
    {
        private readonly string _name, _type;
        private readonly Expression[] _expressions;
        public object? Builder { get; set; }

        public FakeFeature(string name, string type, Expression[]? expressions = null)
        {
            _name = name;
            _type = type;
            _expressions = expressions ?? System.Array.Empty<Expression>();
        }

        public override string Name => _name;
        public override string FeatureType => _type;
        public override Expression[] GetExpressions() => _expressions;
    }

    internal sealed class FakeFeatureCollection : NXOpen.Features.FeatureCollection
    {
        private readonly NXOpen.Features.Feature[] _items;
        public FakeFeatureCollection(NXOpen.Features.Feature[] items) => _items = items;

        public override NXOpen.Features.Feature[] ToArray() => _items;

        public override ExtrudeBuilder CreateExtrudeBuilder(NXOpen.Features.Feature feature) =>
            (ExtrudeBuilder)((FakeFeature)feature).Builder!;

        public override EdgeBlendBuilder CreateEdgeBlendBuilder(NXOpen.Features.Feature feature) =>
            (EdgeBlendBuilder)((FakeFeature)feature).Builder!;
    }

    internal sealed class FakeComponentAssembly : ComponentAssembly
    {
        public override Component? RootComponent => null; // a leaf part, not an assembly
    }

    internal sealed class FakePart : Part
    {
        private readonly string _leaf;
        private readonly ExpressionCollection _expressions;
        private readonly SketchCollection _sketches;
        private readonly NXOpen.Features.FeatureCollection _features;
        private readonly ComponentAssembly _assembly = new FakeComponentAssembly();

        public FakePart(string leaf, ExpressionCollection expressions, SketchCollection sketches, NXOpen.Features.FeatureCollection features)
        {
            _leaf = leaf;
            _expressions = expressions;
            _sketches = sketches;
            _features = features;
        }

        public override string Leaf => _leaf;
        public override string FullPath => _leaf + ".prt";
        public override PartUnits PartUnits => PartUnits.Millimeters;
        public override ExpressionCollection Expressions => _expressions;
        public override SketchCollection Sketches => _sketches;
        public override NXOpen.Features.FeatureCollection Features => _features;
        public override ComponentAssembly ComponentAssembly => _assembly;
    }

    internal sealed class FakePartCollection : PartCollection
    {
        private readonly Part _work;
        public FakePartCollection(Part work) => _work = work;
        public override Part? Work => _work;
    }

    internal sealed class FakeSession : Session
    {
        private readonly PartCollection _parts;
        public FakeSession(Part work) => _parts = new FakePartCollection(work);
        public override PartCollection Parts => _parts;
    }

    /// <summary>Builds a fake NX scene: a 40x30x50 mm box (sketch + extrude) with a 5 mm
    /// fillet on a top edge, and two user parameters — enough to dry-run the extraction.</summary>
    internal static class FakeNxScene
    {
        public static Session SampleBracket()
        {
            var mm = new FakeUnit("mm");
            var expressions = new FakeExpressionCollection(new Expression[]
            {
                new FakeExpression("width", "40", "Number", 40, mm),
                new FakeExpression("height", "30", "Number", 30, mm),
            });

            // A rectangle in the z=0 plane: four lines sharing corners.
            var l0 = new FakeLine(P(0, 0, 0), P(40, 0, 0));
            var l1 = new FakeLine(P(40, 0, 0), P(40, 30, 0));
            var l2 = new FakeLine(P(40, 30, 0), P(0, 30, 0));
            var l3 = new FakeLine(P(0, 30, 0), P(0, 0, 0));
            var rectangle = new NXObject[] { l0, l1, l2, l3 };
            var sketches = new FakeSketchCollection(new Sketch[] { new FakeSketch("SKETCH_000", rectangle) });

            // Extrude the rectangle 50 mm (section reuses the sketch's curves so it maps to sketch 0).
            var extrude = new FakeFeature("EXTRUDE(1)", "EXTRUDE")
            {
                Builder = new FakeExtrudeBuilder(new FakeSection(rectangle), new FakeLimits(0, 50)),
            };

            // Fillet a top edge (0,0,50)-(40,0,50) at radius 5 mm.
            var fillet = new FakeFeature("EDGE BLEND(2)", "EDGE BLEND",
                new Expression[] { new FakeExpression("radius", "5", "Number", 5, mm, owner: null) })
            {
                Builder = new FakeEdgeBlendBuilder(new FakeScCollector(new NXObject[]
                {
                    new FakeEdge(P(0, 0, 50), P(40, 0, 50)),
                })),
            };

            var features = new FakeFeatureCollection(new NXOpen.Features.Feature[] { extrude, fillet });
            var part = new FakePart("sample-bracket", expressions, sketches, features);
            return new FakeSession(part);
        }

        private static Point3d P(double x, double y, double z) => new Point3d(x, y, z);
    }
}
