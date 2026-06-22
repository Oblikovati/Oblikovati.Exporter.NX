// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Oblikovati.Exporter.NX.Model;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class SketchPlaneMathTests
    {
        private static double Dist2(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1];
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double Dist3(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // The projection into the fitted frame must be an isometry: 2D distances equal the
        // 3D distances for coplanar points (so an extracted profile keeps its true shape),
        // whatever in-plane axis the fit chooses.
        [Theory]
        [MemberData(nameof(CoplanarSquares))]
        public void ProjectionPreservesDistances(double[][] square)
        {
            SketchPlaneFrame frame = SketchPlaneMath.Fit(square);
            for (int i = 0; i < square.Length; i++)
            {
                for (int j = i + 1; j < square.Length; j++)
                {
                    double d2 = Dist2(frame.To2D(square[i]), frame.To2D(square[j]));
                    Assert.Equal(Dist3(square[i], square[j]), d2, 9);
                }
            }
        }

        public static IEnumerable<object[]> CoplanarSquares()
        {
            // A 4x3 rectangle in the z=5 plane.
            yield return new object[]
            {
                new[] { new[] { 0.0, 0, 5 }, new[] { 4.0, 0, 5 }, new[] { 4.0, 3, 5 }, new[] { 0.0, 3, 5 } },
            };
            // A 2x2 square in the y=0 (XZ) plane — a tilted-relative-to-XY case.
            yield return new object[]
            {
                new[] { new[] { 0.0, 0, 0 }, new[] { 2.0, 0, 0 }, new[] { 2.0, 0, 2 }, new[] { 0.0, 0, 2 } },
            };
        }

        [Fact]
        public void AverageIsTheCentroid()
        {
            double[] c = GeometryMath.Average(new[]
            {
                new[] { 0.0, 0, 0 }, new[] { 4.0, 0, 0 }, new[] { 4.0, 2, 0 }, new[] { 0.0, 2, 0 },
            });
            Assert.Equal(new[] { 2.0, 1.0, 0.0 }, c);
        }
    }
}
