# SPDX-License-Identifier: GPL-2.0-only
"""Unit tests for the dependency-free YAML emitter."""
import pytest

from oblikovati_exporter_nx.recipe.yaml_writer import (
    RecipeYamlWriter,
    _format_float,
    _format_string,
)


class _Doc:
    def __init__(self, obj):
        self._obj = obj

    def to_yaml(self):
        return self._obj


def _write(obj):
    return RecipeYamlWriter().write(_Doc(obj))


def test_integer_valued_float_has_no_decimal_point():
    assert _format_float(5.0) == "5"
    assert _format_float(0.0) == "0"
    assert _format_float(-3.0) == "-3"


def test_fractional_float_uses_shortest_repr():
    assert _format_float(0.5) == "0.5"
    assert _format_float(0.1) == "0.1"


def test_non_finite_float_is_rejected():
    with pytest.raises(ValueError):
        _format_float(float("inf"))


def test_plain_strings_are_unquoted():
    assert _format_string("mm") == "mm"
    assert _format_string("mm^2") == "mm^2"
    assert _format_string("box-component:1") == "box-component:1"  # colon, no space
    assert _format_string("width * 2") == "width * 2"


def test_special_strings_are_quoted():
    assert _format_string("") == "''"
    assert _format_string("true") == '"true"'
    assert _format_string("a: b") == '"a: b"'  # colon + space
    assert _format_string(" leading") == '"' + " leading" + '"'
    assert _format_string("40") == '"40"'  # numeric-looking stays a string


def test_bool_and_int_scalars():
    assert _write({"a": True, "b": False, "n": 7}) == "a: true\nb: false\nn: 7\n"


def test_block_sequence_of_scalars():
    assert _write({"v": [1, 2, 3]}) == "v:\n- 1\n- 2\n- 3\n"


def test_sequence_of_mappings_indentation():
    out = _write({"items": [{"id": 1, "x": 2}, {"id": 3, "x": 4}]})
    assert out == "items:\n- id: 1\n  x: 2\n- id: 3\n  x: 4\n"


def test_nested_mapping_and_list_in_seq_item():
    out = _write({"items": [{"name": "e", "pts": [1, 2]}]})
    assert out == "items:\n- name: e\n  pts:\n  - 1\n  - 2\n"


def test_nested_mapping_indents_two_spaces():
    out = _write({"model": {"units": {"length": "mm"}}})
    assert out == "model:\n  units:\n    length: mm\n"
