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

    /// <summary>
    /// A revolve of a sketch profile about the sketch's own centerline (the profile sketch
    /// must contain a line marked <see cref="NxCurve.Centerline"/>). <see cref="AngleDegrees"/>
    /// of 0 means a full revolution.
    /// </summary>
    public sealed class NxRevolve : NxFeature
    {
        public int SketchIndex { get; set; }

        public int ProfileIndex { get; set; }

        public NxOperation Operation { get; set; } = NxOperation.NewBody;

        /// <summary>Swept angle in degrees; 0 means a full 360 revolution.</summary>
        public double AngleDegrees { get; set; }
    }

    /// <summary>
    /// Base of features that replicate earlier features. <see cref="SourceFeatureIndices"/>
    /// are indices into <see cref="NxDocument.Features"/> (resolved to program indices on
    /// translation); they must refer to earlier, translatable features.
    /// </summary>
    public abstract class NxReplicatingFeature : NxFeature
    {
        public System.Collections.Generic.IList<int> SourceFeatureIndices { get; } =
            new System.Collections.Generic.List<int>();
    }

    /// <summary>A rectangular grid pattern. Step vectors are the offset between adjacent copies (mm).</summary>
    public sealed class NxRectangularPattern : NxReplicatingFeature
    {
        public int CountX { get; set; } = 1;

        public int CountY { get; set; } = 1;

        public double[] StepX { get; set; } = { 0, 0, 0 };

        public double[] StepY { get; set; } = { 0, 0, 0 };
    }

    /// <summary>A circular pattern about an axis. AngleDegrees is the total spread (0 = full 360).</summary>
    public sealed class NxCircularPattern : NxReplicatingFeature
    {
        public int Count { get; set; } = 1;

        public double AngleDegrees { get; set; }

        public double[] AxisPoint { get; set; } = { 0, 0, 0 };

        public double[] AxisDir { get; set; } = { 0, 0, 1 };
    }

    /// <summary>A mirror across a plane given by its origin (mm) and unit normal.</summary>
    public sealed class NxMirror : NxReplicatingFeature
    {
        public double[] PlaneOrigin { get; set; } = { 0, 0, 0 };

        public double[] PlaneNormal { get; set; } = { 1, 0, 0 };
    }
}
