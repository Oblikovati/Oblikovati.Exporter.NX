// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Builds a serializable <see cref="OblikovatiDocument"/> from an NX-neutral
    /// <see cref="NxDocument"/>. Part documents are self-contained; assembly documents need
    /// their occurrences' component file names from the tree walk (see DocumentExporter),
    /// so that path uses <see cref="TranslateAssembly"/>.
    /// </summary>
    public sealed class DocumentTranslator
    {
        /// <summary>Translates a part document. Throws for a non-part (use the exporter for assemblies).</summary>
        public OblikovatiDocument Translate(NxDocument document, ExportReport report)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (report == null) throw new ArgumentNullException(nameof(report));

            if (document.Kind != NxDocumentKind.Part)
            {
                throw new NotSupportedException(
                    $"document kind '{document.Kind}' is not a part; assemblies go through DocumentExporter");
            }

            return new OblikovatiDocument
            {
                SchemaVersion = 2,
                DocumentType = (int)NxDocumentKind.Part,
                DisplayName = document.DisplayName,
                Model = TranslatePart(document, report),
            };
        }

        /// <summary>
        /// Builds an assembly document from its display info and the already-resolved
        /// occurrences (their component file names supplied by the tree walk).
        /// </summary>
        public OblikovatiDocument TranslateAssembly(NxDocument document, IReadOnlyList<OccurrenceData> occurrences)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var recipe = new AssemblyRecipe
            {
                Units = new Units { Length = document.LengthUnit, Angle = document.AngleUnit },
            };
            foreach (OccurrenceData occurrence in occurrences)
            {
                recipe.Occurrences.Add(occurrence);
            }

            return new OblikovatiDocument
            {
                SchemaVersion = 2,
                DocumentType = (int)NxDocumentKind.Assembly,
                DisplayName = document.DisplayName,
                Model = recipe,
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

            foreach (NxWorkPlane plane in document.WorkPlanes)
            {
                recipe.WorkFeatures.Add(WorkPlaneTranslator.Translate(plane));
            }

            // One id space across sketches, points and entities (matches the Go codec).
            var ids = new IdAllocator();
            var sketches = new SketchTranslator(ids, report);
            foreach (NxSketch sketch in document.Sketches)
            {
                int sketchId = ids.Next();
                recipe.Sketches.Add(sketches.Translate(sketch, sketchId));
            }

            // Map each IR feature index to its recipe index so patterns/mirror can remap
            // their source program indices, skipping over any feature that was not translated.
            var features = new FeatureTranslator(report);
            var sourceIndex = new Dictionary<int, int>();
            for (int i = 0; i < document.Features.Count; i++)
            {
                FeatureData? translated = features.Translate(document.Features[i], sourceIndex);
                if (translated != null)
                {
                    sourceIndex[i] = recipe.Features.Count;
                    recipe.Features.Add(translated);
                }
            }

            return recipe;
        }
    }
}
