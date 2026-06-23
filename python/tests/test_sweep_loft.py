# SPDX-License-Identifier: GPL-2.0-only
"""Sweep and loft translation, and the YAML writer's nested-sequence (path) emission."""
from oblikovati_exporter_nx.model.feature import NxLoft, NxLoftSection, NxOperation, NxSweep
from oblikovati_exporter_nx.recipe.feature import FeatureData, SweepData
from oblikovati_exporter_nx.recipe.yaml_writer import RecipeYamlWriter
from oblikovati_exporter_nx.translate.feature_translator import FeatureTranslator
from oblikovati_exporter_nx.translate.report import ExportReport


def _translate(feature):
    return FeatureTranslator(ExportReport()).translate(feature, {})


def test_sweep_scales_path_to_centimetres():
    sweep = NxSweep(
        name="S", profile_sketch_index=2, profile_index=1,
        path=[[0, 0, 0], [0, 0, 50]], operation=NxOperation.JOIN,
    )
    data = _translate(sweep)
    assert data.kind == "sweep"
    assert data.payload.sketch == 2
    assert data.payload.profile == 1
    assert data.payload.path == [[0.0, 0.0, 0.0], [0.0, 0.0, 5.0]]  # mm -> cm
    assert data.payload.operation == "join"
    assert data.payload.closed is None


def test_closed_sweep_sets_flag():
    sweep = NxSweep(name="S", path=[[0, 0, 0], [10, 0, 0]], closed=True)
    assert _translate(sweep).payload.closed is True


def test_loft_maps_sections_in_order():
    loft = NxLoft(
        name="L",
        sections=[NxLoftSection(0, 0), NxLoftSection(1, 0), NxLoftSection(2, 3)],
        operation=NxOperation.NEW_BODY,
    )
    data = _translate(loft)
    assert data.kind == "loft"
    assert [(s.sketch, s.profile) for s in data.payload.sections] == [(0, 0), (1, 0), (2, 3)]
    assert data.payload.operation == "newBody"


def test_sweep_path_emitted_as_block_nested_sequence():
    doc = FeatureData(
        kind="sweep", payload_alias="sweep",
        payload=SweepData(sketch=0, profile=0, path=[[0, 0, 0], [0, 0, 5]]),
    )

    class _Wrapper:
        def to_yaml(self):
            return {"f": doc.to_yaml()}

    out = RecipeYamlWriter().write(_Wrapper())
    assert "    path:\n    - - 0\n      - 0\n      - 0\n    - - 0\n      - 0\n      - 5\n" in out
