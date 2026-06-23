# SPDX-License-Identifier: GPL-2.0-only
"""Builds a serializable OblikovatiDocument from an NX-neutral NxDocument.

Part documents are self-contained; assembly documents need their occurrences' component
file names from the tree walk (see DocumentExporter), so that path uses
``translate_assembly``.
"""
from __future__ import annotations

from typing import Dict, List

from ..model.document import NxDocument, NxDocumentKind
from ..recipe.document import (
    AssemblyRecipe,
    OblikovatiDocument,
    OccurrenceData,
    PartRecipe,
    Units,
)
from . import parameter_translator, workplane_translator
from .feature_translator import FeatureTranslator
from .id_allocator import IdAllocator
from .report import ExportReport
from .sketch_translator import SketchTranslator


class DocumentTranslator:
    def translate(self, document: NxDocument, report: ExportReport) -> OblikovatiDocument:
        """Translates a part document. Raises for a non-part (use the exporter for assemblies)."""
        if document.kind != NxDocumentKind.PART:
            raise ValueError(
                f"document kind '{document.kind}' is not a part; "
                "assemblies go through DocumentExporter"
            )
        return OblikovatiDocument(
            schema_version=2,
            document_type=int(NxDocumentKind.PART),
            display_name=document.display_name,
            model=self._translate_part(document, report),
        )

    def translate_assembly(
        self, document: NxDocument, occurrences: List[OccurrenceData]
    ) -> OblikovatiDocument:
        """Builds an assembly document from its display info and resolved occurrences."""
        recipe = AssemblyRecipe(
            units=Units(length=document.length_unit, angle=document.angle_unit),
            occurrences=list(occurrences),
        )
        return OblikovatiDocument(
            schema_version=2,
            document_type=int(NxDocumentKind.ASSEMBLY),
            display_name=document.display_name,
            model=recipe,
        )

    @staticmethod
    def _translate_part(document: NxDocument, report: ExportReport) -> PartRecipe:
        recipe = PartRecipe(
            units=Units(length=document.length_unit, angle=document.angle_unit)
        )

        for expression in document.expressions:
            recipe.parameters.append(parameter_translator.translate(expression))

        for plane in document.work_planes:
            recipe.work_features.append(workplane_translator.translate(plane))

        # One id space across sketches, points and entities (matches the Go codec).
        ids = IdAllocator()
        sketches = SketchTranslator(ids, report)
        for sketch in document.sketches:
            sketch_id = ids.next()
            recipe.sketches.append(sketches.translate(sketch, sketch_id))

        # Map each IR feature index to its recipe index so patterns/mirror can remap their
        # source program indices, skipping over any feature that was not translated.
        features = FeatureTranslator(report)
        source_index: Dict[int, int] = {}
        for i, feature in enumerate(document.features):
            translated = features.translate(feature, source_index)
            if translated is not None:
                source_index[i] = len(recipe.features)
                recipe.features.append(translated)

        return recipe
