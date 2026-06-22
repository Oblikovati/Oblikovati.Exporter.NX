// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using NXOpen;
using NXOpen.Features;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Reads a part's feature history into IR features. Dispatches on the NX feature type and
    /// reads each feature through its builder (the documented way to read an existing
    /// feature): scalar parameters from the feature's expressions, selected geometry from the
    /// builder's collectors, converted to the geometric descriptors the dress-up features
    /// carry (ADR-0040). UNVERIFIED — needs a real NX session; the builder member shapes are a
    /// best-effort match to the NXOpen API.
    ///
    /// Done here: edge blend (fillet), chamfer, shell. Deferred (documented), because they
    /// need feature-specific builder APIs and/or sketch cross-referencing only resolvable
    /// against live NX: extrude/revolve (section → sketch index + limits), hole
    /// (HolePackageBuilder), draft, and the patterns/mirror families.
    /// </summary>
    public static class FeatureExtractor
    {
        public static void Extract(Part part, NxDocument document)
        {
            foreach (Feature feature in part.Features.ToArray())
            {
                NxFeature? extracted = ExtractFeature(part, feature);
                if (extracted != null)
                {
                    document.Features.Add(extracted);
                }
            }
        }

        private static NxFeature? ExtractFeature(Part part, Feature feature)
        {
            switch (feature.FeatureType)
            {
                case "EDGE BLEND":
                    return Fillet(part, feature);
                case "CHAMFER":
                    return Chamfer(part, feature);
                case "HOLLOW":
                case "SHELL":
                    return Shell(part, feature);
                default:
                    return null; // extrude/revolve/hole/draft/pattern: live-NX completion
            }
        }

        private static NxFillet Fillet(Part part, Feature feature)
        {
            EdgeBlendBuilder builder = part.Features.CreateEdgeBlendBuilder(feature);
            try
            {
                var fillet = new NxFillet { Name = feature.Name, RadiusMm = FirstValue(feature) };
                foreach (Edge edge in EdgesOf(builder.Edges))
                {
                    fillet.Edges.Add(NxEdgeGeometry.Describe(edge));
                }

                return fillet;
            }
            finally
            {
                builder.Destroy();
            }
        }

        private static NxChamfer Chamfer(Part part, Feature feature)
        {
            ChamferBuilder builder = part.Features.CreateChamferBuilder(feature);
            try
            {
                var chamfer = new NxChamfer { Name = feature.Name, DistanceMm = FirstValue(feature) };
                foreach (Edge edge in EdgesOf(builder.Edges))
                {
                    chamfer.Edges.Add(NxEdgeGeometry.Describe(edge));
                }

                return chamfer;
            }
            finally
            {
                builder.Destroy();
            }
        }

        private static NxShell Shell(Part part, Feature feature)
        {
            ShellBuilder builder = part.Features.CreateShellBuilder(feature);
            try
            {
                var shell = new NxShell { Name = feature.Name, ThicknessMm = FirstValue(feature) };
                foreach (Face face in FacesOf(builder.PiercedFaces))
                {
                    shell.RemovedFaces.Add(NxFaceGeometry.Describe(face));
                }

                return shell;
            }
            finally
            {
                builder.Destroy();
            }
        }

        // A feature's primary scalar (a blend's radius, a chamfer's distance, …) in base units (mm).
        private static double FirstValue(Feature feature)
        {
            Expression[] expressions = feature.GetExpressions();
            return expressions.Length > 0 ? expressions[0].Value : 0;
        }

        private static IEnumerable<Edge> EdgesOf(ScCollector collector)
        {
            foreach (NXObject obj in collector.GetObjects())
            {
                if (obj is Edge edge)
                {
                    yield return edge;
                }
            }
        }

        private static IEnumerable<Face> FacesOf(ScCollector collector)
        {
            foreach (NXObject obj in collector.GetObjects())
            {
                if (obj is Face face)
                {
                    yield return face;
                }
            }
        }
    }
}
