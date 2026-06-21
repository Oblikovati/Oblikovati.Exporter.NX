// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Translates one NX sketch into an Oblikovati <see cref="SketchData"/>: shared
    /// points (via <see cref="SharedPointBuilder"/>), curve entities, geometric
    /// constraints, and parameter-linked dimensions. Lengths convert from the IR's
    /// millimetres to the recipe's centimetre database unit.
    /// </summary>
    public sealed class SketchTranslator
    {
        private const double MmToCm = 0.1;

        private readonly IdAllocator _ids;
        private readonly ExportReport _report;

        public SketchTranslator(IdAllocator ids, ExportReport report)
        {
            _ids = ids ?? throw new ArgumentNullException(nameof(ids));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public SketchData Translate(NxSketch sketch, int sketchId)
        {
            var points = new SketchPointTable();
            points.Build(sketch, _ids);

            var data = new SketchData
            {
                Id = sketchId,
                Name = sketch.Name.Length == 0 ? null : sketch.Name,
                Plane = TranslatePlane(sketch),
            };
            foreach (PointData p in points.Points)
            {
                data.Points.Add(p);
            }

            var entityIds = AddEntities(sketch, points, data);
            AddConstraints(sketch, points, entityIds, data);
            AddDimensions(sketch, points, entityIds, data);
            return data;
        }

        private static PlaneData TranslatePlane(NxSketch sketch)
        {
            return new PlaneData
            {
                Origin = Scale(sketch.Origin),
                XAxis = (double[])sketch.XAxis.Clone(),
                YAxis = (double[])sketch.YAxis.Clone(),
            };
        }

        // Allocates an entity id per curve (after points) and emits its EntityData.
        private Dictionary<long, int> AddEntities(NxSketch sketch, SketchPointTable points, SketchData data)
        {
            var entityIds = new Dictionary<long, int>();
            foreach (NxCurve curve in sketch.Curves)
            {
                int id = _ids.Next();
                entityIds[curve.Id] = id;
                data.Entities.Add(BuildEntity(id, curve, points));
            }

            return entityIds;
        }

        private static EntityData BuildEntity(int id, NxCurve curve, SketchPointTable points)
        {
            var entity = new EntityData
            {
                Id = id,
                Kind = KindName(curve.Kind),
                Construction = curve.Construction ? true : (bool?)null,
            };
            switch (curve.Kind)
            {
                case NxCurveKind.Line:
                    entity.Points.Add(points.PointId(new NxPointRef(curve.Id, NxCurvePointRole.Start)));
                    entity.Points.Add(points.PointId(new NxPointRef(curve.Id, NxCurvePointRole.End)));
                    entity.Centerline = curve.Centerline ? true : (bool?)null;
                    break;
                case NxCurveKind.Circle:
                    entity.Points.Add(points.PointId(new NxPointRef(curve.Id, NxCurvePointRole.Center)));
                    entity.Radius = curve.Radius * MmToCm;
                    break;
                default: // Arc
                    entity.Points.Add(points.PointId(new NxPointRef(curve.Id, NxCurvePointRole.Center)));
                    entity.Points.Add(points.PointId(new NxPointRef(curve.Id, NxCurvePointRole.Start)));
                    entity.Points.Add(points.PointId(new NxPointRef(curve.Id, NxCurvePointRole.End)));
                    entity.Ccw = curve.Ccw ? true : (bool?)null;
                    break;
            }

            return entity;
        }

        private void AddConstraints(
            NxSketch sketch, SketchPointTable points, Dictionary<long, int> entityIds, SketchData data)
        {
            foreach (NxSketchConstraint c in sketch.Constraints)
            {
                ConstraintData? row = BuildConstraint(c, points, entityIds);
                if (row != null)
                {
                    data.Constraints.Add(row);
                }
            }
        }

        private ConstraintData? BuildConstraint(
            NxSketchConstraint c, SketchPointTable points, Dictionary<long, int> entityIds)
        {
            var row = new ConstraintData { Kind = ConstraintName(c.Kind) };
            switch (c.Kind)
            {
                case NxConstraintKind.Coincident:
                    // Distinct endpoints joined by an explicit coincidence (engine format).
                    row.Points.Add(points.PointId(c.Points[0]));
                    row.Points.Add(points.PointId(c.Points[1]));
                    return row;
                case NxConstraintKind.Horizontal:
                case NxConstraintKind.Vertical:
                    // NX applies these to a line; Oblikovati constrains the line's two endpoints.
                    long line = c.Curves[0];
                    row.Points.Add(points.PointId(new NxPointRef(line, NxCurvePointRole.Start)));
                    row.Points.Add(points.PointId(new NxPointRef(line, NxCurvePointRole.End)));
                    return row;
                case NxConstraintKind.Parallel:
                case NxConstraintKind.Perpendicular:
                case NxConstraintKind.Collinear:
                case NxConstraintKind.EqualLength:
                case NxConstraintKind.Concentric:
                case NxConstraintKind.EqualRadius:
                case NxConstraintKind.Tangent:
                    foreach (long id in c.Curves)
                    {
                        row.Curves.Add(entityIds[id]);
                    }

                    return row;
                case NxConstraintKind.PointOnLine:
                case NxConstraintKind.Midpoint:
                    row.Points.Add(points.PointId(c.Points[0]));
                    row.Curves.Add(entityIds[c.Curves[0]]);
                    return row;
                case NxConstraintKind.Fix:
                    row.Points.Add(points.PointId(c.Points[0]));
                    return row;
                default:
                    _report.Unsupported("sketch-constraint", c.Kind.ToString());
                    return null;
            }
        }

        private void AddDimensions(
            NxSketch sketch, SketchPointTable points, Dictionary<long, int> entityIds, SketchData data)
        {
            foreach (NxSketchDimension d in sketch.Dimensions)
            {
                data.Dimensions.Add(BuildDimension(d, points, entityIds));
            }
        }

        private static DimensionData BuildDimension(
            NxSketchDimension d, SketchPointTable points, Dictionary<long, int> entityIds)
        {
            var row = new DimensionData
            {
                Kind = DimensionName(d.Kind),
                Expression = d.Expression,
                Driven = d.Driven ? true : (bool?)null,
            };
            if (d.Kind == NxDimensionKind.Distance)
            {
                foreach (NxPointRef p in d.Points)
                {
                    row.Points.Add(points.PointId(p));
                }
            }
            else
            {
                foreach (long id in d.Curves)
                {
                    row.Curves.Add(entityIds[id]);
                }
            }

            return row;
        }

        private static double[] Scale(double[] v) => new[] { v[0] * MmToCm, v[1] * MmToCm, v[2] * MmToCm };

        private static string KindName(NxCurveKind kind)
        {
            switch (kind)
            {
                case NxCurveKind.Line: return "line";
                case NxCurveKind.Circle: return "circle";
                default: return "arc";
            }
        }

        private static string ConstraintName(NxConstraintKind kind)
        {
            switch (kind)
            {
                case NxConstraintKind.Coincident: return "coincident";
                case NxConstraintKind.Horizontal: return "horizontal";
                case NxConstraintKind.Vertical: return "vertical";
                case NxConstraintKind.Parallel: return "parallel";
                case NxConstraintKind.Perpendicular: return "perpendicular";
                case NxConstraintKind.Collinear: return "collinear";
                case NxConstraintKind.EqualLength: return "equalLength";
                case NxConstraintKind.Concentric: return "concentric";
                case NxConstraintKind.EqualRadius: return "equalRadius";
                case NxConstraintKind.Tangent: return "tangent";
                case NxConstraintKind.PointOnLine: return "pointOnLine";
                case NxConstraintKind.Midpoint: return "midpoint";
                case NxConstraintKind.Fix: return "fix";
                default: return kind.ToString();
            }
        }

        private static string DimensionName(NxDimensionKind kind)
        {
            switch (kind)
            {
                case NxDimensionKind.Distance: return "distance";
                case NxDimensionKind.Radius: return "radius";
                case NxDimensionKind.Diameter: return "diameter";
                default: return "angle";
            }
        }
    }
}
