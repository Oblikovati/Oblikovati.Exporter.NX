// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Model;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class GeometryMathTests
    {
        [Fact]
        public void MidpointAverages()
        {
            double[] m = GeometryMath.Midpoint(new double[] { 0, 0, 0 }, new double[] { 4, 0, 10 });
            Assert.Equal(new[] { 2.0, 0.0, 5.0 }, m);
        }

        [Fact]
        public void UnitDirectionNormalizes()
        {
            double[] d = GeometryMath.UnitDirection(new double[] { 1, 1, 1 }, new double[] { 1, 1, 4 });
            Assert.Equal(new[] { 0.0, 0.0, 1.0 }, d);
        }

        [Fact]
        public void UnitDirectionOfCoincidentPointsIsZero()
        {
            double[] d = GeometryMath.UnitDirection(new double[] { 2, 3, 4 }, new double[] { 2, 3, 4 });
            Assert.Equal(new double[] { 0, 0, 0 }, d);
        }
    }
}
