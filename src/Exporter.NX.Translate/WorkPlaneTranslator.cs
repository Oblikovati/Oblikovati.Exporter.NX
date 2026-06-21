// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Maps an NX datum plane to a fixed-frame work plane: its origin (mm -> cm) and two
    /// in-plane unit axes. A fixed frame carries the datum's solved geometry faithfully
    /// without re-deriving the NX construction.
    /// </summary>
    public static class WorkPlaneTranslator
    {
        private const double MmToCm = 0.1;

        public static WorkFeatureData Translate(NxWorkPlane plane)
        {
            return new WorkFeatureData
            {
                Collection = "plane",
                Kind = "fixed-frame",
                Position = Scale(plane.Origin),
                XAxis = (double[])plane.XAxis.Clone(),
                YAxis = (double[])plane.YAxis.Clone(),
            };
        }

        private static double[] Scale(double[] v) => new[] { v[0] * MmToCm, v[1] * MmToCm, v[2] * MmToCm };
    }
}
