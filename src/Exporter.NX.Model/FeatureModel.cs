// SPDX-License-Identifier: GPL-2.0-only
namespace Oblikovati.Exporter.NX.Model
{
    /// <summary>Boolean operation a feature performs against existing bodies.</summary>
    public enum NxOperation
    {
        NewBody,
        Join,
        Cut,
        Intersect,
    }

    /// <summary>Which way a single-distance extent grows from its sketch plane.</summary>
    public enum NxExtentDirection
    {
        Positive,
        Negative,
        Symmetric,
    }

    /// <summary>Base of an extracted NX feature. The translator dispatches on the concrete type.</summary>
    public abstract class NxFeature
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// An extrude of a sketch profile. <see cref="SketchIndex"/> is the index into
    /// <see cref="NxDocument.Sketches"/>; <see cref="ProfileIndex"/> selects a detected
    /// region of that sketch. Lengths are millimetres (IR contract); the depth is an
    /// evaluated value (the recipe extent is not parameter-driven yet).
    /// </summary>
    public sealed class NxExtrude : NxFeature
    {
        public int SketchIndex { get; set; }

        public int ProfileIndex { get; set; }

        public NxOperation Operation { get; set; } = NxOperation.NewBody;

        public NxExtentDirection Direction { get; set; } = NxExtentDirection.Positive;

        public double Distance { get; set; }

        /// <summary>Second-direction distance for an asymmetric two-sided extrude (mm).</summary>
        public double SecondDistance { get; set; }

        /// <summary>Draft/taper angle in degrees (0 for a straight extrude).</summary>
        public double TaperDegrees { get; set; }
    }
}
