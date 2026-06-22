// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using NXOpen;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Builds a geometric face descriptor (centroid) from an NX face, for dress-up face
    /// selections (shell/draft/hole placement, ADR-0040). The centroid is the average of the
    /// face's edge vertices — the face centre for a planar face and a stable representative
    /// otherwise. The normal is left unset (Oblikovati's resolver matches a centroid-only
    /// descriptor by nearness, which is unambiguous for distinct faces); a normal would need
    /// the UF face-props API, a refinement for symmetric geometry.
    /// </summary>
    public static class NxFaceGeometry
    {
        public static NxFaceDescriptor Describe(Face face)
        {
            var points = new List<double[]>();
            foreach (Edge edge in face.GetEdges())
            {
                edge.GetVertices(out Point3d a, out Point3d b);
                points.Add(new[] { a.X, a.Y, a.Z });
                points.Add(new[] { b.X, b.Y, b.Z });
            }

            return new NxFaceDescriptor { Centroid = GeometryMath.Average(points), Normal = new double[] { 0, 0, 0 } };
        }
    }
}
