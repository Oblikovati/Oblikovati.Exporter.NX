// SPDX-License-Identifier: GPL-2.0-only
using System;

// Compile-only facade of the NXOpen members the extraction layer reads. Mirrors the real
// NXOpen .NET signatures so the adapter, built against this in CI, binds the genuine
// NXOpen.dll at load time in NX. Every member throws — nothing here runs outside NX.
namespace NXOpen
{
    /// <summary>Stub of NXOpen.Point3d (a value struct of doubles).</summary>
    public struct Point3d
    {
        public double X;
        public double Y;
        public double Z;

        public Point3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>Stub of NXOpen.Unit (a measurement unit; Symbol is e.g. "mm").</summary>
    public class Unit : NXObject
    {
        public virtual string Symbol => throw StubError();
    }

    /// <summary>Stub of NXOpen.Expression.</summary>
    public class Expression : NXObject
    {
        /// <summary>The right-hand side of the equation (e.g. "40" or "width*2").</summary>
        public virtual string RightHandSide => throw StubError();

        /// <summary>"Number", "String", "Boolean", "Integer", "Vector", "Point", "List".</summary>
        public virtual string Type => throw StubError();

        /// <summary>The expression's unit (number expressions only).</summary>
        public virtual Unit Units => throw StubError();

        /// <summary>The feature that owns this expression, or null for a user parameter.</summary>
        public virtual Features.Feature? GetOwningFeature() => throw StubError();
    }

    /// <summary>Stub of NXOpen.ExpressionCollection.</summary>
    public class ExpressionCollection
    {
        public virtual Expression[] ToArray() => throw new InvalidOperationException(StubMessage);

        internal const string StubMessage =
            "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll";
    }

    /// <summary>Stub of a topological Edge.</summary>
    public class Edge : NXObject
    {
        /// <summary>Returns the edge's two end vertices.</summary>
        public virtual void GetVertices(out Point3d vertex1, out Point3d vertex2) => throw StubError();
    }

    /// <summary>Stub of a topological Face.</summary>
    public class Face : NXObject
    {
        public virtual Tag Tag => throw StubError();
    }

    /// <summary>Stub of NXOpen.Body.</summary>
    public class Body : NXObject
    {
        public virtual bool IsSolidBody => throw StubError();

        public virtual Edge[] GetEdges() => throw StubError();

        public virtual Face[] GetFaces() => throw StubError();
    }

    /// <summary>Stub of NXOpen.BodyCollection.</summary>
    public class BodyCollection
    {
        public virtual Body[] ToArray() => throw new InvalidOperationException(ExpressionCollection.StubMessage);
    }

    /// <summary>NXOpen.Tag is an opaque object handle (a ulong typedef in the real API).</summary>
    public struct Tag
    {
        public ulong Value;
    }
}

namespace NXOpen.Features
{
    /// <summary>Stub of NXOpen.Features.Feature (the base of every modeling feature).</summary>
    public class Feature : NXObject
    {
        /// <summary>The feature's type name, e.g. "EXTRUDE", "SIMPLE HOLE", "EDGE BLEND".</summary>
        public virtual string FeatureType => throw StubError();
    }

    /// <summary>Stub of NXOpen.Features.FeatureCollection.</summary>
    public class FeatureCollection
    {
        public virtual Feature[] ToArray() =>
            throw new System.InvalidOperationException(ExpressionCollection.StubMessage);
    }
}
