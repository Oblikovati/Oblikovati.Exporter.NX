// SPDX-License-Identifier: GPL-2.0-only
using System;

namespace Oblikovati.Exporter.NX.Model
{
    /// <summary>
    /// Small pure vector helpers used to build geometric descriptors from NX vertex/point
    /// data. Kept free of NXOpen so the descriptor math is unit-testable (the surrounding
    /// extraction that reads NXOpen is not).
    /// </summary>
    public static class GeometryMath
    {
        /// <summary>Midpoint of two 3D points.</summary>
        public static double[] Midpoint(double[] a, double[] b) =>
            new[] { (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2 };

        /// <summary>Component-wise average of 3D points (the empty set averages to the origin).</summary>
        public static double[] Average(System.Collections.Generic.IReadOnlyList<double[]> points)
        {
            if (points.Count == 0)
            {
                return new double[] { 0, 0, 0 };
            }

            double x = 0, y = 0, z = 0;
            foreach (double[] p in points)
            {
                x += p[0];
                y += p[1];
                z += p[2];
            }

            return new[] { x / points.Count, y / points.Count, z / points.Count };
        }

        /// <summary>
        /// Unit vector from <paramref name="from"/> to <paramref name="to"/>; the zero
        /// vector when the points coincide (a degenerate edge has no meaningful direction).
        /// </summary>
        public static double[] UnitDirection(double[] from, double[] to)
        {
            double dx = to[0] - from[0], dy = to[1] - from[1], dz = to[2] - from[2];
            double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len == 0)
            {
                return new double[] { 0, 0, 0 };
            }

            return new[] { dx / len, dy / len, dz / len };
        }
    }
}
