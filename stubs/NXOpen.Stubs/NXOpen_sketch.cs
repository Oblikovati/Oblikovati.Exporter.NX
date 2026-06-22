// SPDX-License-Identifier: GPL-2.0-only
// Compile-only facade of the NXOpen sketch read surface.
namespace NXOpen
{
    /// <summary>Stub of NXOpen.SketchCollection.</summary>
    public class SketchCollection
    {
        public virtual Sketch[] ToArray() => throw new System.InvalidOperationException(
            "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll");
    }

    /// <summary>Stub of NXOpen.Sketch.</summary>
    public class Sketch : NXObject
    {
        /// <summary>All geometry objects (lines, arcs, circles, …) in the sketch.</summary>
        public virtual NXObject[] GetAllGeometry() => throw StubError();
    }

    /// <summary>Stub of NXOpen.Line (a sketch/model line).</summary>
    public class Line : NXObject
    {
        public virtual Point3d StartPoint => throw StubError();

        public virtual Point3d EndPoint => throw StubError();
    }

    /// <summary>Stub of NXOpen.Arc (a sketch/model arc or circle).</summary>
    public class Arc : NXObject
    {
        public virtual Point3d CenterPoint => throw StubError();

        public virtual double Radius => throw StubError();

        /// <summary>2*pi for a full circle; less for an arc.</summary>
        public virtual double EndAngle => throw StubError();

        public virtual double StartAngle => throw StubError();
    }
}
