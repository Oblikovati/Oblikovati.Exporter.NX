// SPDX-License-Identifier: GPL-2.0-only
using System;
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

        /// <summary>Returns the recipe feature for <paramref name="feature"/>, or null if unsupported.</summary>
        public FeatureData? Translate(NxFeature feature)
        {
            switch (feature)
            {
                case NxExtrude extrude:
                    return TranslateExtrude(extrude);
                default:
                    _report.Unsupported("feature", feature.GetType().Name);
                    return null;
            }
        }

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
