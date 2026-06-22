// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the NXOpen feature read surface used by the extractor. The builder
// shapes are a best-effort match to the real API (this whole layer is unverified without NX).
namespace NXOpen
{
    /// <summary>Stub of NXOpen.ScCollector (a smart selection collector of objects).</summary>
    public class ScCollector
    {
        public virtual NXObject[] GetObjects() => throw new System.InvalidOperationException(StubMessage);

        internal const string StubMessage =
            "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll";
    }

    /// <summary>Stub of NXOpen.Vector3d (a direction/vector value struct).</summary>
    public struct Vector3d
    {
        public double X;
        public double Y;
        public double Z;
    }

    /// <summary>Stub of NXOpen.Section (a profile of curves feeding extrude/revolve).</summary>
    public class Section : NXObject
    {
        /// <summary>The curves that make up the section.</summary>
        public virtual NXObject[] GetOutputCurves() => throw StubError();
    }

    /// <summary>Stub of NXOpen.Axis (a positioned direction, e.g. a revolve axis).</summary>
    public class Axis : NXObject
    {
        public virtual Point3d Point => throw StubError();

        public virtual Vector3d Direction => throw StubError();
    }
}

namespace NXOpen.Features
{
    /// <summary>Base of the feature builders, with the Destroy() every builder needs.</summary>
    public abstract class FeatureBuilder
    {
        public virtual void Destroy() => throw new System.InvalidOperationException(
            "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll");
    }

    /// <summary>
    /// Stub of the edge-blend (fillet) builder. Verified against the NXOpen .NET reference:
    /// blended edges are organised into CHAINSETS — each chainset pairs an ScCollector of
    /// edges with its radius Expression — read via GetNumberOfValidChainsets / GetChainset
    /// (not a flat .Edges collector).
    /// </summary>
    public class EdgeBlendBuilder : FeatureBuilder
    {
        public virtual int GetNumberOfValidChainsets() =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual void GetChainset(int index, out ScCollector collector, out Expression radius) =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);
    }

    /// <summary>Stub of the chamfer builder.</summary>
    public class ChamferBuilder : FeatureBuilder
    {
        public virtual ScCollector Edges =>
            throw new System.InvalidOperationException("NXOpen stub: run inside NX");
    }

    /// <summary>Stub of the shell (hollow) builder.</summary>
    public class ShellBuilder : FeatureBuilder
    {
        public virtual ScCollector PiercedFaces =>
            throw new System.InvalidOperationException("NXOpen stub: run inside NX");
    }

    /// <summary>One extrude/revolve limit: a value (distance in mm, or angle in radians).</summary>
    public class Limit
    {
        public virtual Expression Value =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);
    }

    /// <summary>Start/end limits of an extrude or revolve.</summary>
    public class Limits
    {
        public virtual Limit StartExtend =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Limit EndExtend =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);
    }

    /// <summary>Stub of the extrude builder.</summary>
    public class ExtrudeBuilder : FeatureBuilder
    {
        public virtual Section Section =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Limits Limits =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);
    }

    /// <summary>Stub of the revolve builder (limits hold the start/end angle in radians).</summary>
    public class RevolveBuilder : FeatureBuilder
    {
        public virtual Section Section =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Limits Limits =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Axis Axis =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);
    }

    /// <summary>Stub of the draft builder.</summary>
    public class DraftBuilder : FeatureBuilder
    {
        public virtual ScCollector FaceCollector =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Vector3d PullDirection =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);
    }

    /// <summary>
    /// Stub of the pattern-feature builder, flattened to the inputs the extractor reads (the
    /// real PatternService layout API is far richer). LayoutType is "Linear" or "Circular".
    /// </summary>
    public class PatternFeatureBuilder : FeatureBuilder
    {
        public virtual Feature[] GetSourceFeatures() =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual string LayoutType =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        // Linear layout.
        public virtual int XCount => throw new System.InvalidOperationException(ScCollector.StubMessage);
        public virtual int YCount => throw new System.InvalidOperationException(ScCollector.StubMessage);
        public virtual Vector3d XDirection => throw new System.InvalidOperationException(ScCollector.StubMessage);
        public virtual Vector3d YDirection => throw new System.InvalidOperationException(ScCollector.StubMessage);
        public virtual double XPitch => throw new System.InvalidOperationException(ScCollector.StubMessage);
        public virtual double YPitch => throw new System.InvalidOperationException(ScCollector.StubMessage);

        // Circular layout.
        public virtual int CircularCount => throw new System.InvalidOperationException(ScCollector.StubMessage);
        public virtual double CircularAngle => throw new System.InvalidOperationException(ScCollector.StubMessage);
        public virtual Point3d AxisPoint => throw new System.InvalidOperationException(ScCollector.StubMessage);
        public virtual Vector3d AxisDirection => throw new System.InvalidOperationException(ScCollector.StubMessage);
    }

    /// <summary>Stub of the mirror-feature builder, flattened to the extractor's inputs.</summary>
    public class MirrorBuilder : FeatureBuilder
    {
        public virtual Feature[] GetSourceFeatures() =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Point3d PlaneOrigin =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Vector3d PlaneNormal =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);
    }

    /// <summary>Stub of the hole (package) builder.</summary>
    public class HolePackageBuilder : FeatureBuilder
    {
        public virtual Face PlacementFace =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Expression Diameter =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual Expression Depth =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);

        public virtual bool ThroughAll =>
            throw new System.InvalidOperationException(ScCollector.StubMessage);
    }
}
