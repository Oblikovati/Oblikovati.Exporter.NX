// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using NXOpen;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Reads a part's sketches into the IR. The sketch plane is fitted from the curves' 3D
    /// points (avoiding the uncertain NX sketch-plane API); points project into that frame as
    /// 2D coordinates. Lines and full circles are extracted; coincidence is inferred from
    /// endpoints that meet, so profiles close in Oblikovati (which is what makes a profile
    /// extrudable). Partial arcs, splines, and NX's explicit constraints/dimensions are not
    /// yet read — flagged for live-NX completion (the geometry is positioned correctly; the
    /// missing dimensions are the parametric refinement). UNVERIFIED: needs a real NX session.
    /// </summary>
    public static class SketchExtractor
    {
        private const double CoincidenceTol = 1e-4; // mm, in sketch 2D

        public static void Extract(Part part, NxDocument document, IDictionary<NXObject, int> curveToSketch)
        {
            foreach (Sketch sketch in part.Sketches.ToArray())
            {
                NXObject[] geometry = sketch.GetAllGeometry();
                NxSketch? extracted = ExtractOne(sketch, geometry);
                if (extracted == null)
                {
                    continue;
                }

                int index = document.Sketches.Count;
                document.Sketches.Add(extracted);
                // Map this sketch's curves so a feature's section resolves to its sketch index.
                foreach (NXObject obj in geometry)
                {
                    curveToSketch[obj] = index;
                }
            }
        }

        private static NxSketch? ExtractOne(Sketch sketch, NXObject[] geometry)
        {
            SketchPlaneFrame frame = SketchPlaneMath.Fit(CollectPoints(geometry));
            var result = new NxSketch
            {
                Name = sketch.Name,
                Origin = frame.Origin,
                XAxis = frame.XAxis,
                YAxis = frame.YAxis,
            };

            long nextId = 1;
            foreach (NXObject obj in geometry)
            {
                switch (obj)
                {
                    case Line line:
                        result.Curves.Add(LineCurve(nextId++, line, frame));
                        break;
                    case Arc arc when IsFullCircle(arc):
                        result.Curves.Add(CircleCurve(nextId++, arc, frame));
                        break;
                }
            }

            InferCoincidences(result);
            return result.Curves.Count == 0 ? null : result;
        }

        private static IReadOnlyList<double[]> CollectPoints(NXObject[] geometry)
        {
            var points = new List<double[]>();
            foreach (NXObject obj in geometry)
            {
                if (obj is Line line)
                {
                    points.Add(P(line.StartPoint));
                    points.Add(P(line.EndPoint));
                }
                else if (obj is Arc arc)
                {
                    points.Add(P(arc.CenterPoint));
                }
            }

            return points;
        }

        private static NxCurve LineCurve(long id, Line line, SketchPlaneFrame frame) => new NxCurve
        {
            Id = id,
            Kind = NxCurveKind.Line,
            Start = frame.To2D(P(line.StartPoint)),
            End = frame.To2D(P(line.EndPoint)),
        };

        private static NxCurve CircleCurve(long id, Arc arc, SketchPlaneFrame frame) => new NxCurve
        {
            Id = id,
            Kind = NxCurveKind.Circle,
            Center = frame.To2D(P(arc.CenterPoint)),
            Radius = arc.Radius,
        };

        // Emit a coincident constraint for each pair of line endpoints that meet, so the
        // profile closes (mirrors how the engine records coincidence between distinct points).
        private static void InferCoincidences(NxSketch sketch)
        {
            var slots = new List<(NxPointRef Ref, double[] Pt)>();
            foreach (NxCurve c in sketch.Curves)
            {
                if (c.Kind != NxCurveKind.Line)
                {
                    continue;
                }

                slots.Add((new NxPointRef(c.Id, NxCurvePointRole.Start), c.Start));
                slots.Add((new NxPointRef(c.Id, NxCurvePointRole.End), c.End));
            }

            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (slots[i].Ref.CurveId == slots[j].Ref.CurveId)
                    {
                        continue;
                    }

                    if (Distance2D(slots[i].Pt, slots[j].Pt) <= CoincidenceTol)
                    {
                        var con = new NxSketchConstraint { Kind = NxConstraintKind.Coincident };
                        con.Points.Add(slots[i].Ref);
                        con.Points.Add(slots[j].Ref);
                        sketch.Constraints.Add(con);
                    }
                }
            }
        }

        private static bool IsFullCircle(Arc arc) => Math.Abs((arc.EndAngle - arc.StartAngle) - 2 * Math.PI) < 1e-6;

        private static double[] P(Point3d p) => new[] { p.X, p.Y, p.Z };

        private static double Distance2D(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1];
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
