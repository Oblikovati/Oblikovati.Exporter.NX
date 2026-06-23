# SPDX-License-Identifier: GPL-2.0-only
"""Covers each sketch constraint family and the arc entity translation path."""
import pytest

from oblikovati_exporter_nx.model.sketch import (
    NxConstraintKind,
    NxCurve,
    NxCurveKind,
    NxCurvePointRole,
    NxPointRef,
    NxSketch,
    NxSketchConstraint,
)
from oblikovati_exporter_nx.translate.id_allocator import IdAllocator
from oblikovati_exporter_nx.translate.report import ExportReport
from oblikovati_exporter_nx.translate.sketch_translator import SketchTranslator


def _two_line_sketch():
    sketch = NxSketch(name="L")
    sketch.curves.append(NxCurve(id=1, kind=NxCurveKind.LINE, start=[0, 0], end=[10, 0]))
    sketch.curves.append(NxCurve(id=2, kind=NxCurveKind.LINE, start=[0, 5], end=[10, 5]))
    return sketch


def _translate(sketch):
    ids = IdAllocator()
    return SketchTranslator(ids, ExportReport()).translate(sketch, ids.next())


@pytest.mark.parametrize(
    "kind,name",
    [
        (NxConstraintKind.PARALLEL, "parallel"),
        (NxConstraintKind.PERPENDICULAR, "perpendicular"),
        (NxConstraintKind.COLLINEAR, "collinear"),
        (NxConstraintKind.EQUAL_LENGTH, "equalLength"),
        (NxConstraintKind.TANGENT, "tangent"),
    ],
)
def test_two_curve_constraints(kind, name):
    sketch = _two_line_sketch()
    constraint = NxSketchConstraint(kind=kind)
    constraint.curves.extend([1, 2])
    sketch.constraints.append(constraint)
    data = _translate(sketch)
    row = data.constraints[0]
    assert row.kind == name
    assert len(row.curves) == 2
    assert row.points == []


def test_point_on_line_uses_point_and_curve():
    sketch = _two_line_sketch()
    constraint = NxSketchConstraint(kind=NxConstraintKind.POINT_ON_LINE)
    constraint.points.append(NxPointRef(1, NxCurvePointRole.START))
    constraint.curves.append(2)
    sketch.constraints.append(constraint)
    row = _translate(sketch).constraints[0]
    assert row.kind == "pointOnLine"
    assert len(row.points) == 1 and len(row.curves) == 1


def test_fix_uses_single_point():
    sketch = _two_line_sketch()
    constraint = NxSketchConstraint(kind=NxConstraintKind.FIX)
    constraint.points.append(NxPointRef(1, NxCurvePointRole.START))
    sketch.constraints.append(constraint)
    row = _translate(sketch).constraints[0]
    assert row.kind == "fix"
    assert len(row.points) == 1 and row.curves == []


def test_arc_entity_emits_center_start_end_and_ccw():
    sketch = NxSketch(name="A")
    arc = NxCurve(id=1, kind=NxCurveKind.ARC, center=[0, 0], start=[10, 0], end=[0, 10], ccw=True)
    sketch.curves.append(arc)
    data = _translate(sketch)
    entity = data.entities[0]
    assert entity.kind == "arc"
    assert len(entity.points) == 3  # center, start, end
    assert entity.ccw is True


def test_construction_curve_flag_is_emitted():
    sketch = NxSketch(name="C")
    sketch.curves.append(
        NxCurve(id=1, kind=NxCurveKind.LINE, start=[0, 0], end=[1, 0], construction=True)
    )
    entity = _translate(sketch).entities[0]
    assert entity.construction is True
