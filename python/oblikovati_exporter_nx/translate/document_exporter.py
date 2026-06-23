# SPDX-License-Identifier: GPL-2.0-only
"""Walks a document tree and produces one TranslatedDocument per document.

A part becomes one ``.opd``; an assembly becomes one ``.oad`` plus the component
documents it references (recursively). Components shared by several occurrences are
exported once (deduped by reference), and the assembly's occurrences reference them by
owner-relative file name, matching Oblikovati's reference graph.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, List, Set

from ..model.document import NxDocument, NxDocumentKind
from ..recipe.document import OblikovatiDocument
from . import occurrence_translator
from .document_translator import DocumentTranslator
from .report import ExportReport


@dataclass
class TranslatedDocument:
    """One translated document and the file name it should be written to."""

    file_name: str
    document: OblikovatiDocument


class DocumentExporter:
    def __init__(self, translator: DocumentTranslator) -> None:
        self._translator = translator

    def export(self, root: NxDocument, report: ExportReport) -> List[TranslatedDocument]:
        files: List[TranslatedDocument] = []
        file_names: Dict[int, str] = {}  # keyed by object identity (id())
        used_names: Set[str] = set()  # lowercased, for case-insensitive dedup
        self._emit(root, files, file_names, used_names, report)
        return files

    # Returns the file name the document was (or will be) written to, emitting it once.
    def _emit(
        self,
        doc: NxDocument,
        files: List[TranslatedDocument],
        file_names: Dict[int, str],
        used_names: Set[str],
        report: ExportReport,
    ) -> str:
        existing = file_names.get(id(doc))
        if existing is not None:
            return existing

        file_name = _unique_name(doc, used_names)
        file_names[id(doc)] = file_name

        if doc.kind == NxDocumentKind.ASSEMBLY:
            occurrences = []
            for occurrence in doc.occurrences:
                child_file = self._emit(occurrence.component, files, file_names, used_names, report)
                occurrences.append(occurrence_translator.translate(occurrence, child_file))
            files.append(
                TranslatedDocument(file_name, self._translator.translate_assembly(doc, occurrences))
            )
        else:
            files.append(TranslatedDocument(file_name, self._translator.translate(doc, report)))

        return file_name


def _unique_name(doc: NxDocument, used: Set[str]) -> str:
    ext = ".oad" if doc.kind == NxDocumentKind.ASSEMBLY else ".opd"
    base = doc.display_name or "untitled"
    candidate = base + ext
    n = 2
    while candidate.lower() in used:
        candidate = f"{base}_{n}{ext}"
        n += 1
    used.add(candidate.lower())
    return candidate
