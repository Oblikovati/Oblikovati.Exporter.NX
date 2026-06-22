// SPDX-License-Identifier: GPL-2.0-only
using NXOpen;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Builds a geometric edge descriptor (midpoint + direction) from an NX edge — the
    /// selection form a dress-up (fillet/chamfer) carries so Oblikovati can rebind it
    /// without lineage keys (ADR-0040). The midpoint/direction come from the edge's end
    /// vertices: exact for a straight edge, and a stable representative + sign-agnostic
    /// hint for a curved one (the resolver also uses tolerance and uniqueness).
    /// </summary>
    public static class NxEdgeGeometry
    {
        public static NxEdgeDescriptor Describe(Edge edge)
        {
            edge.GetVertices(out Point3d a, out Point3d b);
            double[] pa = { a.X, a.Y, a.Z };
            double[] pb = { b.X, b.Y, b.Z };
            return new NxEdgeDescriptor
            {
                Midpoint = GeometryMath.Midpoint(pa, pb),
                Direction = GeometryMath.UnitDirection(pa, pb),
            };
        }
    }
}
