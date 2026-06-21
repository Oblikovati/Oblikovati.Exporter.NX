// SPDX-License-Identifier: GPL-2.0-only
namespace Oblikovati.Exporter.NX.Model
{
    /// <summary>
    /// One placed component in an assembly: the referenced document (a part or sub-assembly,
    /// shared by reference so instances dedup to one exported file), and its placement as a
    /// position (mm) plus a row-major 3x3 rotation. The translator assembles these into the
    /// 16-cell transform Oblikovati stores.
    /// </summary>
    public sealed class NxOccurrence
    {
        public string Name { get; set; } = string.Empty;

        public NxDocument Component { get; set; } = new NxDocument();

        /// <summary>Translation in model space (mm).</summary>
        public double[] Position { get; set; } = { 0, 0, 0 };

        /// <summary>Row-major 3x3 rotation; identity by default.</summary>
        public double[] Rotation { get; set; } = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
    }
}
