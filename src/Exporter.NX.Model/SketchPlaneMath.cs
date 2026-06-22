// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;

namespace Oblikovati.Exporter.NX.Model
{
    /// <summary>A fitted sketch plane frame: an origin and orthonormal in-plane axes (model space).</summary>
    public sealed class SketchPlaneFrame
    {
        public double[] Origin { get; set; } = { 0, 0, 0 };

        public double[] XAxis { get; set; } = { 1, 0, 0 };

        public double[] YAxis { get; set; } = { 0, 1, 0 };

        /// <summary>Projects a model-space point onto the frame's 2D (u, v) coordinates.</summary>
        public double[] To2D(double[] p)
        {
            double[] d = SketchPlaneMath.Sub(p, Origin);
            return new[] { SketchPlaneMath.Dot(d, XAxis), SketchPlaneMath.Dot(d, YAxis) };
        }
    }

    /// <summary>
    /// Pure geometry for sketch extraction: fitting a plane frame to the 3D points of an NX
    /// sketch's curves and projecting points into it. Kept NXOpen-free so it is unit-testable
    /// (the surrounding extraction that reads NXOpen curves is not). Robust enough for the
    /// planar sketches NX produces; a non-planar point set fits to its first valid triangle.
    /// </summary>
    public static class SketchPlaneMath
    {
        /// <summary>
        /// Fits a plane frame to points: origin at their centroid, normal from the first
        /// non-degenerate triangle, X axis toward the farthest point, Y = normal x X. Falls
        /// back to the world XY frame when the points are collinear/degenerate.
        /// </summary>
        public static SketchPlaneFrame Fit(IReadOnlyList<double[]> points)
        {
            double[] origin = GeometryMath.Average(points);
            double[]? normal = FirstNormal(origin, points);
            if (normal == null)
            {
                return new SketchPlaneFrame { Origin = origin };
            }

            double[] x = FarthestInPlane(origin, normal, points);
            double[] y = Normalize(Cross(normal, x));
            return new SketchPlaneFrame { Origin = origin, XAxis = x, YAxis = y };
        }

        // The unit normal of the first non-collinear triple about the origin, or null.
        private static double[]? FirstNormal(double[] origin, IReadOnlyList<double[]> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    double[] n = Cross(Sub(points[i], origin), Sub(points[j], origin));
                    if (Length(n) > 1e-9)
                    {
                        return Normalize(n);
                    }
                }
            }

            return null;
        }

        // A unit in-plane X axis toward the point farthest from the origin (projected into the plane).
        private static double[] FarthestInPlane(double[] origin, double[] normal, IReadOnlyList<double[]> points)
        {
            double[] best = { 1, 0, 0 };
            double bestLen = 0;
            foreach (double[] p in points)
            {
                double[] d = Sub(p, origin);
                double[] inPlane = Sub(d, Scale(normal, Dot(d, normal)));
                double len = Length(inPlane);
                if (len > bestLen)
                {
                    bestLen = len;
                    best = inPlane;
                }
            }

            return bestLen > 1e-9 ? Normalize(best) : AnyPerpendicular(normal);
        }

        private static double[] AnyPerpendicular(double[] n)
        {
            double[] seed = Math.Abs(n[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            return Normalize(Cross(n, seed));
        }

        public static double[] Sub(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };

        public static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        public static double[] Cross(double[] a, double[] b) => new[]
        {
            a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0],
        };

        public static double[] Scale(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };

        public static double Length(double[] a) => Math.Sqrt(Dot(a, a));

        public static double[] Normalize(double[] a)
        {
            double len = Length(a);
            return len == 0 ? new double[] { 0, 0, 0 } : Scale(a, 1 / len);
        }
    }
}
