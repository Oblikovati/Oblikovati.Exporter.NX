# SPDX-License-Identifier: GPL-2.0-only
"""Walks an NX component tree into IR occurrences.

Each component becomes an NxOccurrence referencing the NX-neutral document of its
prototype part (extracted via the supplied part extractor, deduped by prototype so a part
placed N times exports once). A sub-assembly component recurses; a leaf component extracts
its part. The orientation's columns are the placed X/Y/Z axes (NX convention), so the
row-major 3x3 the IR carries is filled column-wise. ``GetPosition()`` returns
(origin, orientation) as a tuple in NXOpen Python.

UNVERIFIED: needs a real NX session.
"""
from __future__ import annotations

from typing import Callable, Dict

from ..model.document import NxDocument, NxDocumentKind, NxOccurrence


class ComponentExtractor:
    def __init__(self, extract_part: Callable[[object], NxDocument]) -> None:
        self._extract_part = extract_part
        self._docs: Dict[int, NxDocument] = {}  # keyed by prototype .Tag

    def occurrence(self, component) -> NxOccurrence:
        origin, orientation = component.GetPosition()
        return NxOccurrence(
            name=component.DisplayName,
            component=self._document_for(component.Prototype),
            position=[origin.X, origin.Y, origin.Z],
            rotation=_row_major(orientation),
        )

    def _document_for(self, prototype) -> NxDocument:
        existing = self._docs.get(prototype.Tag)
        if existing is not None:
            return existing

        root = prototype.ComponentAssembly.RootComponent
        children = list(root.GetChildren()) if root is not None else []
        doc = self._subassembly(prototype, children) if children else self._extract_part(prototype)
        self._docs[prototype.Tag] = doc
        return doc

    def _subassembly(self, prototype, children) -> NxDocument:
        doc = NxDocument(
            display_name=prototype.Leaf,
            kind=NxDocumentKind.ASSEMBLY,
            length_unit=_length_unit_of(prototype),
        )
        for child in children:
            doc.occurrences.append(self.occurrence(child))
        return doc


# Columns of the NX orientation are the placed axes; lay them out row-major.
def _row_major(m) -> list:
    return [
        m.Xx, m.Yx, m.Zx,
        m.Xy, m.Yy, m.Zy,
        m.Xz, m.Yz, m.Zz,
    ]


def _length_unit_of(part) -> str:
    import NXOpen

    return "in" if part.PartUnits == NXOpen.Part.Units.Inches else "mm"
