// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Translates NX history features into Oblikovati recipe features. Unsupported kinds
    /// are recorded in the report and skipped (never STEP-substituted). Currently handles
    /// extrudes; revolve/sweep/loft and datums follow.
    /// </summary>
    public sealed class FeatureTranslator
    {
        private const double MmToCm = 0.1;
        private const double DegToRad = Math.PI / 180.0;

        private readonly ExportReport _report;

        public FeatureTranslator(ExportReport report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        /// <summary>
        /// Returns the recipe feature for <paramref name="feature"/>, or null if unsupported
        /// or (for a pattern/mirror) one of its sources was itself skipped.
        /// <paramref name="sourceIndex"/> maps an IR feature index to the recipe feature index.
        /// </summary>
        public FeatureData? Translate(NxFeature feature, IReadOnlyDictionary<int, int> sourceIndex)
        {
            switch (feature)
            {
                case NxExtrude extrude:
                    return TranslateExtrude(extrude);
                case NxRevolve revolve:
                    return TranslateRevolve(revolve);
                case NxRectangularPattern rect:
                    return TranslateRectPattern(rect, sourceIndex);
                case NxCircularPattern circ:
                    return TranslateCircPattern(circ, sourceIndex);
                case NxMirror mirror:
                    return TranslateMirror(mirror, sourceIndex);
                default:
                    _report.Unsupported("feature", feature.GetType().Name);
                    return null;
            }
        }

        private FeatureData? TranslateRectPattern(NxRectangularPattern pattern, IReadOnlyDictionary<int, int> sourceIndex)
        {
            if (!TryResolveSources(pattern, sourceIndex, out var sources))
            {
                return null;
            }

            var payload = new RectPatternData
            {
                CountX = pattern.CountX,
                CountY = pattern.CountY,
                StepX = Scale(pattern.StepX),
                StepY = Scale(pattern.StepY),
            };
            AddRange(payload.Source, sources);
            return new FeatureData { Kind = "rectangular-pattern", Name = NameOf(pattern), RectangularPattern = payload };
        }

        private FeatureData? TranslateCircPattern(NxCircularPattern pattern, IReadOnlyDictionary<int, int> sourceIndex)
        {
            if (!TryResolveSources(pattern, sourceIndex, out var sources))
            {
                return null;
            }

            var payload = new CircPatternData
            {
                Count = pattern.Count,
                Angle = (pattern.AngleDegrees == 0 ? 360.0 : pattern.AngleDegrees) * DegToRad,
                AxisPoint = Scale(pattern.AxisPoint),
                AxisDir = (double[])pattern.AxisDir.Clone(),
            };
            AddRange(payload.Source, sources);
            return new FeatureData { Kind = "circular-pattern", Name = NameOf(pattern), CircularPattern = payload };
        }

        private FeatureData? TranslateMirror(NxMirror mirror, IReadOnlyDictionary<int, int> sourceIndex)
        {
            if (!TryResolveSources(mirror, sourceIndex, out var sources))
            {
                return null;
            }

            var payload = new MirrorData
            {
                Origin = Scale(mirror.PlaneOrigin),
                Normal = (double[])mirror.PlaneNormal.Clone(),
            };
            AddRange(payload.Source, sources);
            return new FeatureData { Kind = "mirror", Name = NameOf(mirror), Mirror = payload };
        }

        // Maps a replicating feature's IR source indices to recipe program indices. Fails
        // (reports + returns false) if any source was skipped, since the pattern can't bind.
        private bool TryResolveSources(
            NxReplicatingFeature feature, IReadOnlyDictionary<int, int> sourceIndex, out List<int> resolved)
        {
            resolved = new List<int>(feature.SourceFeatureIndices.Count);
            foreach (int ir in feature.SourceFeatureIndices)
            {
                if (!sourceIndex.TryGetValue(ir, out int recipeIndex))
                {
                    _report.Warn($"{feature.GetType().Name} '{feature.Name}' references feature {ir}, " +
                        "which was not translated; skipped");
                    return false;
                }

                resolved.Add(recipeIndex);
            }

            return true;
        }

        private static void AddRange(IList<int> target, IEnumerable<int> values)
        {
            foreach (int v in values) target.Add(v);
        }

        private static string? NameOf(NxFeature feature) =>
            feature.Name.Length == 0 ? null : feature.Name;

        private static double[] Scale(double[] v) => new[] { v[0] * MmToCm, v[1] * MmToCm, v[2] * MmToCm };

        private static FeatureData TranslateExtrude(NxExtrude extrude)
        {
            var payload = new ExtrudeData
            {
                Sketch = extrude.SketchIndex,
                Operation = OperationName(extrude.Operation),
                Extent = "distance",
                Direction = DirectionName(extrude.Direction),
                Distance = extrude.Distance * MmToCm,
                Distance2 = extrude.SecondDistance != 0 ? extrude.SecondDistance * MmToCm : (double?)null,
                Taper = extrude.TaperDegrees != 0 ? extrude.TaperDegrees * DegToRad : (double?)null,
            };
            payload.Profiles.Add(extrude.ProfileIndex);

            return new FeatureData
            {
                Kind = "extrude",
                Name = extrude.Name.Length == 0 ? null : extrude.Name,
                Extrude = payload,
            };
        }

        private static FeatureData TranslateRevolve(NxRevolve revolve)
        {
            // Own-centerline mode: the profile sketch carries the axis as a centerline,
            // so no axis fields are emitted. Angle 0 (full revolution) is left unset.
            var payload = new RevolveData
            {
                Sketch = revolve.SketchIndex,
                Profile = revolve.ProfileIndex,
                Operation = OperationName(revolve.Operation),
                Angle = revolve.AngleDegrees != 0 ? revolve.AngleDegrees * DegToRad : (double?)null,
            };

            return new FeatureData
            {
                Kind = "revolve",
                Name = revolve.Name.Length == 0 ? null : revolve.Name,
                Revolve = payload,
            };
        }

        private static string OperationName(NxOperation operation)
        {
            switch (operation)
            {
                case NxOperation.Join: return "join";
                case NxOperation.Cut: return "cut";
                case NxOperation.Intersect: return "intersect";
                default: return "newBody";
            }
        }

        private static string DirectionName(NxExtentDirection direction)
        {
            switch (direction)
            {
                case NxExtentDirection.Negative: return "negative";
                case NxExtentDirection.Symmetric: return "symmetric";
                default: return "positive";
            }
        }
    }
}
