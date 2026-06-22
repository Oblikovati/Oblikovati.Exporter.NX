// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the NXOpen feature read surface used by the extractor. The builder
// shapes are a best-effort match to the real API (this whole layer is unverified without NX).
namespace NXOpen
{
    /// <summary>Stub of NXOpen.ScCollector (a smart selection collector of objects).</summary>
    public class ScCollector
    {
        public virtual NXObject[] GetObjects() => throw new System.InvalidOperationException(
            "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll");
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

    /// <summary>Stub of the edge-blend (fillet) builder.</summary>
    public class EdgeBlendBuilder : FeatureBuilder
    {
        public virtual ScCollector Edges =>
            throw new System.InvalidOperationException("NXOpen stub: run inside NX");
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
}
