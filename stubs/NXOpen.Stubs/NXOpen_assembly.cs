// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the NXOpen assembly read surface.
namespace NXOpen
{
    /// <summary>Stub of NXOpen.Matrix3x3 (a row/column orientation; columns are X,Y,Z axes).</summary>
    public struct Matrix3x3
    {
        public double Xx, Xy, Xz;
        public double Yx, Yy, Yz;
        public double Zx, Zy, Zz;
    }
}

namespace NXOpen.Assemblies
{
    /// <summary>Stub of NXOpen.Assemblies.Component.</summary>
    public class Component : NXObject
    {
        /// <summary>Instance display name (e.g. "bracket:1").</summary>
        public virtual string DisplayName => throw StubError();

        /// <summary>The prototype part this component instances.</summary>
        public virtual Part Prototype => throw StubError();

        /// <summary>Child components of a sub-assembly (empty for a leaf).</summary>
        public virtual Component[] GetChildren() => throw StubError();

        /// <summary>The component's origin and orientation in its parent's space.</summary>
        public virtual void GetPosition(out Point3d origin, out Matrix3x3 orientation) => throw StubError();
    }

    /// <summary>Stub of NXOpen.Assemblies.ComponentAssembly.</summary>
    public class ComponentAssembly
    {
        /// <summary>The root component, or null when the part is not an assembly.</summary>
        public virtual Component? RootComponent => throw new System.InvalidOperationException(
            "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll");
    }
}
