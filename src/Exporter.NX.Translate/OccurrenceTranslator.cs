// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Builds an <see cref="OccurrenceData"/> from an NX occurrence and the file name its
    /// component was exported to. The placement becomes a 16-cell row-major transform with
    /// the rotation in the upper-left 3x3 and the translation (mm -> cm) in cells 3, 7, 11 —
    /// the layout math.Matrix4 uses (translation lives at m[3], m[7], m[11]).
    /// </summary>
    public static class OccurrenceTranslator
    {
        private const double MmToCm = 0.1;

        public static OccurrenceData Translate(NxOccurrence occurrence, string componentFileName)
        {
            return new OccurrenceData
            {
                Name = occurrence.Name,
                Component = componentFileName,
                Transform = BuildTransform(occurrence.Rotation, occurrence.Position),
            };
        }

        private static double[] BuildTransform(double[] r, double[] p)
        {
            return new[]
            {
                r[0], r[1], r[2], p[0] * MmToCm,
                r[3], r[4], r[5], p[1] * MmToCm,
                r[6], r[7], r[8], p[2] * MmToCm,
                0.0,  0.0,  0.0,  1.0,
            };
        }
    }
}
