// SPDX-License-Identifier: GPL-2.0-only
using System;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Top of the translation core: turns an NX-neutral <see cref="NxDocument"/> into a
    /// serializable <see cref="OblikovatiDocument"/>. Assembly translation (.oad)
    /// arrives in M6; until then a non-part document raises a clear error.
    /// </summary>
    public sealed class DocumentTranslator
    {
        public OblikovatiDocument Translate(NxDocument document, ExportReport report)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (report == null) throw new ArgumentNullException(nameof(report));

            if (document.Kind != NxDocumentKind.Part)
            {
                throw new NotSupportedException(
                    $"document kind '{document.Kind}' is not translatable yet; expected Part");
            }

            return new OblikovatiDocument
            {
                SchemaVersion = 2,
                DocumentType = (int)NxDocumentKind.Part,
                DisplayName = document.DisplayName,
                Model = TranslatePart(document, report),
            };
        }

        private static PartRecipe TranslatePart(NxDocument document, ExportReport report)
        {
            var recipe = new PartRecipe
            {
                Units = new Units
                {
                    Length = document.LengthUnit,
                    Angle = document.AngleUnit,
                },
            };

            foreach (NxExpression expression in document.Expressions)
            {
                recipe.Parameters.Add(ParameterTranslator.Translate(expression));
            }

            // One id space across sketches, points and entities (matches the Go codec).
            var ids = new IdAllocator();
            var sketches = new SketchTranslator(ids, report);
            foreach (NxSketch sketch in document.Sketches)
            {
                int sketchId = ids.Next();
                recipe.Sketches.Add(sketches.Translate(sketch, sketchId));
            }

            var features = new FeatureTranslator(report);
            foreach (NxFeature feature in document.Features)
            {
                FeatureData? translated = features.Translate(feature);
                if (translated != null)
                {
                    recipe.Features.Add(translated);
                }
            }

            return recipe;
        }
    }
}
