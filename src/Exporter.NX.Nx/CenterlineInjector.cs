// SPDX-License-Identifier: GPL-2.0-only
using NXOpen;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Adds the NX revolve axis to its profile sketch as a centerline. Oblikovati revolves
    /// about the sketch's own centerline (a line flagged <see cref="NxCurve.Centerline"/>),
    /// so the axis — a point + direction in model space — is projected into the sketch's
    /// fitted frame and added as a 2D centerline line. A no-op when the axis is not in-plane
    /// (its projection degenerates to a point), which leaves the revolve to fail honestly.
    /// </summary>
    public static class CenterlineInjector
    {
        private const double AxisHalfLength = 500.0; // mm; a centerline is an axis, length is cosmetic

        public static void Inject(NxSketch sketch, Axis axis)
        {
            var frame = new SketchPlaneFrame { Origin = sketch.Origin, XAxis = sketch.XAxis, YAxis = sketch.YAxis };
            double[] point = { axis.Point.X, axis.Point.Y, axis.Point.Z };
            double[] dir = SketchPlaneMath.Normalize(new[] { axis.Direction.X, axis.Direction.Y, axis.Direction.Z });

            double[] a = frame.To2D(SketchPlaneMath.Sub(point, SketchPlaneMath.Scale(dir, AxisHalfLength)));
            double[] b = frame.To2D(SketchPlaneMath.Sub(point, SketchPlaneMath.Scale(dir, -AxisHalfLength)));
            if (Distance2D(a, b) < 1e-6)
            {
                return; // axis is perpendicular to the sketch — not a usable in-plane centerline
            }

            sketch.Curves.Add(new NxCurve
            {
                Id = NextCurveId(sketch),
                Kind = NxCurveKind.Line,
                Start = a,
                End = b,
                Centerline = true,
            });
        }

        private static long NextCurveId(NxSketch sketch)
        {
            long max = 0;
            foreach (NxCurve c in sketch.Curves)
            {
                if (c.Id > max)
                {
                    max = c.Id;
                }
            }

            return max + 1;
        }

        private static double Distance2D(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1];
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
