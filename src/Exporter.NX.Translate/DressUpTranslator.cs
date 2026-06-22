// SPDX-License-Identifier: GPL-2.0-only
using System;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Translates NX dress-up features (fillet/chamfer/shell/draft/hole) into recipe features
    /// whose edge/face selections are GEOMETRIC descriptors (ADR-0040) — the path that lets
    /// the exporter place them without Oblikovati lineage keys. Lengths convert mm -> cm; the
    /// draft angle converts degrees -> radians.
    /// </summary>
    public static class DressUpTranslator
    {
        private const double MmToCm = 0.1;
        private const double DegToRad = Math.PI / 180.0;

        public static FeatureData Fillet(NxFillet fillet)
        {
            var payload = new EdgeDressData { Value = fillet.RadiusMm * MmToCm };
            AddEdges(payload, fillet.Edges);
            return new FeatureData { Kind = "fillet", Name = NameOf(fillet), Fillet = payload };
        }

        public static FeatureData Chamfer(NxChamfer chamfer)
        {
            var payload = new EdgeDressData { Value = chamfer.DistanceMm * MmToCm };
            AddEdges(payload, chamfer.Edges);
            return new FeatureData { Kind = "chamfer", Name = NameOf(chamfer), Chamfer = payload };
        }

        public static FeatureData Shell(NxShell shell)
        {
            var payload = new FaceDressData { Value = shell.ThicknessMm * MmToCm };
            AddFaces(payload, shell.RemovedFaces);
            return new FeatureData { Kind = "shell", Name = NameOf(shell), Shell = payload };
        }

        public static FeatureData Draft(NxDraft draft)
        {
            var payload = new FaceDressData
            {
                Value = draft.AngleDegrees * DegToRad,
                Pull = (double[])draft.Pull.Clone(),
            };
            AddFaces(payload, draft.Faces);
            return new FeatureData { Kind = "draft", Name = NameOf(draft), Draft = payload };
        }

        public static FeatureData Hole(NxHole hole)
        {
            var payload = new HoleData
            {
                Diameter = hole.DiameterMm * MmToCm,
                Depth = hole.DepthMm * MmToCm,
                ThroughAll = hole.ThroughAll ? true : (bool?)null,
                Type = "drilled",
                GeomFace = FaceRef(hole.PlacementFace),
            };
            return new FeatureData { Kind = "hole", Name = NameOf(hole), Hole = payload };
        }

        private static void AddEdges(EdgeDressData payload, System.Collections.Generic.IEnumerable<NxEdgeDescriptor> edges)
        {
            foreach (NxEdgeDescriptor e in edges)
            {
                payload.GeomEdges.Add(new GeomEdgeRefData
                {
                    Midpoint = Scale(e.Midpoint),
                    Direction = (double[])e.Direction.Clone(),
                });
            }
        }

        private static void AddFaces(FaceDressData payload, System.Collections.Generic.IEnumerable<NxFaceDescriptor> faces)
        {
            foreach (NxFaceDescriptor f in faces)
            {
                payload.GeomFaces.Add(FaceRef(f));
            }
        }

        private static GeomFaceRefData FaceRef(NxFaceDescriptor f) =>
            new GeomFaceRefData { Centroid = Scale(f.Centroid), Normal = (double[])f.Normal.Clone() };

        private static double[] Scale(double[] v) => new[] { v[0] * MmToCm, v[1] * MmToCm, v[2] * MmToCm };

        private static string? NameOf(NxFeature feature) => feature.Name.Length == 0 ? null : feature.Name;
    }
}
