// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using NXOpen;
using NXOpen.Features;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Reads a part's feature history into IR features. Dispatches on the NX feature type and
    /// reads each feature through its builder (the documented way to read an existing
    /// feature): scalar parameters from the feature's expressions/builder, selected geometry
    /// from the builder's collectors → the geometric descriptors the dress-ups carry
    /// (ADR-0040). A sketch-based feature resolves its section to the IR sketch index via the
    /// curve→sketch map built during sketch extraction. UNVERIFIED — needs a real NX session;
    /// builder member shapes are a best-effort match to the NXOpen API.
    ///
    /// Done: extrude, revolve, fillet, chamfer, shell, draft, hole. Still deferred (need
    /// feature-specific APIs / live NX): the patterns/mirror families and partial-arc/spline
    /// sketch geometry. Profile index defaults to 0 (NX section → Oblikovati region index has
    /// no stable mapping).
    /// </summary>
    public static class FeatureExtractor
    {
        private const double RadToDeg = 180.0 / Math.PI;

        public static void Extract(Part part, NxDocument document, IReadOnlyDictionary<NXObject, int> curveToSketch)
        {
            foreach (Feature feature in part.Features.ToArray())
            {
                NxFeature? extracted = ExtractFeature(part, feature, document, curveToSketch);
                if (extracted != null)
                {
                    document.Features.Add(extracted);
                }
            }
        }

        private static NxFeature? ExtractFeature(
            Part part, Feature feature, NxDocument document, IReadOnlyDictionary<NXObject, int> curveToSketch)
        {
            switch (feature.FeatureType)
            {
                case "EXTRUDE":
                    return Extrude(part, feature, curveToSketch);
                case "REVOLVE":
                case "REVOLVED":
                    return Revolve(part, feature, document, curveToSketch);
                case "EDGE BLEND":
                    return Fillet(part, feature);
                case "CHAMFER":
                    return Chamfer(part, feature);
                case "HOLLOW":
                case "SHELL":
                    return Shell(part, feature);
                case "DRAFT":
                    return Draft(part, feature);
                case "SIMPLE HOLE":
                case "HOLE PACKAGE":
                case "HOLE":
                    return Hole(part, feature);
                default:
                    return null; // patterns/mirror & exotic geometry: live-NX completion
            }
        }

        private static NxFeature? Extrude(Part part, Feature feature, IReadOnlyDictionary<NXObject, int> curveToSketch)
        {
            ExtrudeBuilder builder = part.Features.CreateExtrudeBuilder(feature);
            try
            {
                int sketch = SketchIndexOf(builder.Section, curveToSketch);
                if (sketch < 0)
                {
                    return null; // section did not resolve to an extracted sketch
                }

                double start = builder.Limits.StartExtend.Value.Value;
                double end = builder.Limits.EndExtend.Value.Value;
                return new NxExtrude
                {
                    Name = feature.Name,
                    SketchIndex = sketch,
                    ProfileIndex = 0,
                    Operation = NxOperation.NewBody,
                    Distance = end,
                    SecondDistance = start != 0 ? Math.Abs(start) : 0,
                    Direction = start != 0 ? NxExtentDirection.Symmetric : NxExtentDirection.Positive,
                };
            }
            finally
            {
                builder.Destroy();
            }
        }

        private static NxFeature? Revolve(
            Part part, Feature feature, NxDocument document, IReadOnlyDictionary<NXObject, int> curveToSketch)
        {
            RevolveBuilder builder = part.Features.CreateRevolveBuilder(feature);
            try
            {
                int sketch = SketchIndexOf(builder.Section, curveToSketch);
                if (sketch < 0)
                {
                    return null;
                }

                double angle = builder.Limits.EndExtend.Value.Value - builder.Limits.StartExtend.Value.Value;
                CenterlineInjector.Inject(document.Sketches[sketch], builder.Axis);
                return new NxRevolve
                {
                    Name = feature.Name,
                    SketchIndex = sketch,
                    ProfileIndex = 0,
                    Operation = NxOperation.NewBody,
                    AngleDegrees = Math.Abs(angle - 2 * Math.PI) < 1e-6 ? 0 : angle * RadToDeg,
                };
            }
            finally
            {
                builder.Destroy();
            }
        }

        private static NxDraft Draft(Part part, Feature feature)
        {
            DraftBuilder builder = part.Features.CreateDraftBuilder(feature);
            try
            {
                Vector3d pull = builder.PullDirection;
                var draft = new NxDraft
                {
                    Name = feature.Name,
                    AngleDegrees = FirstValue(feature) * RadToDeg,
                    Pull = new[] { pull.X, pull.Y, pull.Z },
                };
                foreach (Face face in FacesOf(builder.FaceCollector))
                {
                    draft.Faces.Add(NxFaceGeometry.Describe(face));
                }

                return draft;
            }
            finally
            {
                builder.Destroy();
            }
        }

        private static NxHole Hole(Part part, Feature feature)
        {
            HolePackageBuilder builder = part.Features.CreateHolePackageBuilder(feature);
            try
            {
                return new NxHole
                {
                    Name = feature.Name,
                    PlacementFace = NxFaceGeometry.Describe(builder.PlacementFace),
                    DiameterMm = builder.Diameter.Value,
                    DepthMm = builder.Depth.Value,
                    ThroughAll = builder.ThroughAll,
                };
            }
            finally
            {
                builder.Destroy();
            }
        }

        // The IR sketch index of the first section curve that belongs to an extracted sketch.
        private static int SketchIndexOf(Section section, IReadOnlyDictionary<NXObject, int> curveToSketch)
        {
            foreach (NXObject curve in section.GetOutputCurves())
            {
                if (curveToSketch.TryGetValue(curve, out int index))
                {
                    return index;
                }
            }

            return -1;
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
