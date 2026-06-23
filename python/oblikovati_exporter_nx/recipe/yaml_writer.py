# SPDX-License-Identifier: GPL-2.0-only
"""A small, dependency-free YAML emitter for Oblikovati recipe documents.

NX's embedded Python interpreter ships no third-party packages (no PyYAML), and the
journal must stay self-contained, so the recipe layer owns its own emitter rather than
depending on a YAML library. The output deliberately matches the block style the C#
add-in's YamlDotNet writer produces (byte-for-byte for the shared fixtures), so both
exporters feed the Oblikovati Go reader identical bytes:

  * mappings are block style, nested keys indented by two spaces;
  * sequences are block style, items aligned with the key that owns them;
  * an integer-valued float prints without a decimal point ("5", not "5.0"), every
    other float uses Python's shortest round-tripping repr (matching .NET's behaviour);
  * only present keys are emitted (the recipe dataclasses apply the omit rules).

The emitter consumes plain Python structures (dict / list / scalar) produced by each
recipe dataclass's ``to_yaml()``; it never touches the dataclasses directly.
"""
from __future__ import annotations

import math
from typing import Any, List, Mapping, Sequence


# Characters that may not start a YAML plain scalar in block context.
_START_INDICATORS = set("!&*?|>%@`\"'#,[]{}")
# Words YAML would otherwise read as a bool/null; emitted quoted so they stay strings.
_RESERVED_WORDS = {
    "true", "false", "null", "yes", "no", "on", "off", "~",
}


class RecipeYamlWriter:
    """Renders an object exposing ``to_yaml()`` to its on-disk YAML text."""

    def write(self, document: Any) -> str:
        root = document.to_yaml()
        if not isinstance(root, dict):
            raise TypeError(f"document root must be a mapping, got {type(root).__name__}")
        lines: List[str] = []
        self._emit_mapping(root, 0, lines)
        return "\n".join(lines) + "\n"

    def _emit_mapping(self, mapping: Mapping[str, Any], indent: int, lines: List[str]) -> None:
        pad = " " * indent
        for key, value in mapping.items():
            if isinstance(value, dict):
                lines.append(f"{pad}{key}:")
                self._emit_mapping(value, indent + 2, lines)
            elif _is_sequence(value):
                lines.append(f"{pad}{key}:")
                self._emit_sequence(value, indent, lines)
            else:
                lines.append(f"{pad}{key}: {self._scalar(value)}")

    def _emit_sequence(self, seq: Sequence[Any], indent: int, lines: List[str]) -> None:
        pad = " " * indent
        for item in seq:
            if isinstance(item, dict):
                self._emit_mapping_item(item, indent, lines)
            elif _is_sequence(item):
                self._emit_sequence_item(item, indent, lines)
            else:
                lines.append(f"{pad}- {self._scalar(item)}")

    # A sequence nested directly inside a sequence (e.g. a sweep path's 3D points): the
    # inner sequence's first scalar shares the "- " line as "- - x", the rest follow at +2.
    def _emit_sequence_item(self, seq: Sequence[Any], indent: int, lines: List[str]) -> None:
        pad = " " * indent
        if not seq:
            lines.append(f"{pad}- []")
            return
        if any(isinstance(el, (dict, list, tuple)) for el in seq):
            raise ValueError("only sequences of scalars may be nested in a sequence")
        lines.append(f"{pad}- - {self._scalar(seq[0])}")
        continuation = " " * (indent + 2)
        for element in seq[1:]:
            lines.append(f"{continuation}- {self._scalar(element)}")

    # One mapping inside a sequence: its first key shares the "- " line; the rest are
    # indented two past the dash, exactly as YamlDotNet lays them out.
    def _emit_mapping_item(self, mapping: Mapping[str, Any], indent: int, lines: List[str]) -> None:
        pad = " " * indent
        items = list(mapping.items())
        if not items:
            lines.append(f"{pad}- {{}}")
            return

        first_key, first_value = items[0]
        if isinstance(first_value, dict):
            lines.append(f"{pad}- {first_key}:")
            self._emit_mapping(first_value, indent + 4, lines)
        elif _is_sequence(first_value):
            lines.append(f"{pad}- {first_key}:")
            self._emit_sequence(first_value, indent + 2, lines)
        else:
            lines.append(f"{pad}- {first_key}: {self._scalar(first_value)}")

        if len(items) > 1:
            self._emit_mapping(dict(items[1:]), indent + 2, lines)

    def _scalar(self, value: Any) -> str:
        if value is None:
            return "null"
        if isinstance(value, bool):
            return "true" if value else "false"
        if isinstance(value, int):
            return str(value)
        if isinstance(value, float):
            return _format_float(value)
        return _format_string(str(value))


def _is_sequence(value: Any) -> bool:
    # str is a Sequence but is a scalar here; bytes likewise.
    return isinstance(value, (list, tuple))


def _format_float(value: float) -> str:
    if not math.isfinite(value):
        raise ValueError(f"cannot serialize non-finite float {value!r}")
    # An integer-valued float prints without a decimal point ("5"), matching the C#
    # writer; the Go reader parses it back into float64 either way.
    if value == int(value):
        return str(int(value))
    return repr(value)


def _format_string(text: str) -> str:
    if text == "":
        return "''"  # YamlDotNet renders the empty string single-quoted
    if not _needs_quote(text):
        return text
    escaped = text.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n").replace("\t", "\\t")
    return f'"{escaped}"'


def _needs_quote(text: str) -> bool:
    """True if ``text`` cannot be a YAML block-context plain scalar.

    A plain scalar may not have surrounding whitespace, look like a number/bool/null,
    start with an indicator character, or contain ``": "`` / a trailing colon / ``" #"``
    (the sequences that would change its meaning). A colon followed by a non-space (as in
    ``box-component:1``) is fine, so such names stay unquoted — matching YamlDotNet.
    """
    if text.strip() != text:
        return True
    if text.lower() in _RESERVED_WORDS or _looks_numeric(text):
        return True
    first = text[0]
    if first in _START_INDICATORS:
        return True
    if first in "-:" and (len(text) == 1 or text[1] == " "):
        return True
    if ": " in text or text.endswith(":") or " #" in text:
        return True
    return any(ord(ch) < 0x20 for ch in text)


def _looks_numeric(text: str) -> bool:
    try:
        float(text)
        return True
    except ValueError:
        return False
