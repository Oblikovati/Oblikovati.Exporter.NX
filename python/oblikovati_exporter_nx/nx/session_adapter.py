# SPDX-License-Identifier: GPL-2.0-only
"""Production NxSession backed by the live NXOpen session.

This is the single class allowed to read NXOpen types; everything downstream consumes the
NX-neutral NxDocument. Reading a feature requires creating its builder, which NX records
on the undo stack; the adapter sets an undo mark before extraction and rolls back to it
afterwards, so an export never dirties the open part (the recommended NXOpen practice for
read-only journals). Never exercised in CI — tests use a fake session instead.

UNVERIFIED: needs a real NX session.
"""
from __future__ import annotations

import os
import tempfile

import NXOpen

from ..model.document import NxDocument, NxDocumentKind
from . import (
    component_extractor,
    expression_extractor,
    feature_extractor,
    sketch_extractor,
)


class NxSessionAdapter:
    def __init__(self, session=None) -> None:
        self._session = session if session is not None else NXOpen.Session.GetSession()

    def extract_work_document(self) -> NxDocument:
        work = self._session.Parts.Work
        if work is None:
            raise RuntimeError("no work part is open in NX")

        mark = self._session.SetUndoMark(
            NXOpen.Session.MarkVisibility.Invisible, "Oblikovati export (read-only)"
        )
        try:
            root = work.ComponentAssembly.RootComponent
            if root is not None and len(root.GetChildren()) > 0:
                return self._extract_assembly(work, root)
            return self._extract_part(work)
        finally:
            # Roll back any builder churn so the export leaves the part untouched.
            self._session.UndoToMark(mark, "Oblikovati export (read-only)")

    def _extract_part(self, part) -> NxDocument:
        document = NxDocument(
            display_name=part.Leaf,
            kind=NxDocumentKind.PART,
            length_unit=_length_unit_of(part),
            angle_unit="deg",
        )
        expression_extractor.extract(part, document)
        # The map lets a sketch-based feature resolve its section to the IR sketch index.
        curve_tag_to_sketch = {}
        sketch_extractor.extract(part, document, curve_tag_to_sketch)
        feature_extractor.extract(part, document, curve_tag_to_sketch)
        return document

    def _extract_assembly(self, part, root) -> NxDocument:
        document = NxDocument(
            display_name=part.Leaf,
            kind=NxDocumentKind.ASSEMBLY,
            length_unit=_length_unit_of(part),
        )
        components = component_extractor.ComponentExtractor(self._extract_part)
        for child in root.GetChildren():
            document.occurrences.append(components.occurrence(child))
        return document

    def output_directory(self) -> str:
        """Where to write exported documents: the work part's directory, or temp if unsaved."""
        work = self._session.Parts.Work
        full_path = work.FullPath if work is not None else ""
        directory = os.path.dirname(full_path) if full_path else ""
        return directory if directory else tempfile.gettempdir()

    def show_message(self, text: str) -> None:
        """Shows the export summary in NX's listing window."""
        window = self._session.ListingWindow
        window.Open()
        window.WriteLine(text)


def _length_unit_of(part) -> str:
    return "in" if part.PartUnits == NXOpen.Part.Units.Inches else "mm"
