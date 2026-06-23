# SPDX-License-Identifier: GPL-2.0-only
"""Covers the full sketch entity set (ellipse / elliptical-arc / spline) and the
symmetry / ground / smooth constraints — the additions that bring NX to the Inventor
exporter's sketch fidelity."""
import math

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


def _translate(sketch):
    ids = IdAllocator()
    return SketchTranslator(ids, ExportReport()).translate(sketch, ids.next())


def test_ellipse_entity_fields_scaled_to_centimetres():
    sketch = NxSketch(name="E")
    sketch.curves.append(
        NxCurve(id=1, kind=NxCurveKind.ELLIPSE, center=[0, 0],
                major_axis=[1, 0], major_radius=40, minor_radius=20)
    )
    entity = _translate(sketch).entities[0]
    assert entity.kind == "ellipse"
    assert len(entity.points) == 1  # center
    assert entity.major_axis == [1, 0]  # direction, not scaled
    assert entity.major_radius == 4.0   # mm -> cm
    assert entity.minor_radius == 2.0
    assert entity.start_angle is None and entity.end_angle is None


def test_elliptical_arc_carries_angles():
    sketch = NxSketch(name="EA")
    sketch.curves.append(
        NxCurve(id=1, kind=NxCurveKind.ELLIPTICAL_ARC, center=[0, 0],
                major_axis=[0, 1], major_radius=30, minor_radius=10,
                start_angle=0.0, end_angle=math.pi)
    )
    entity = _translate(sketch).entities[0]
    assert entity.kind == "ellipticalArc"
    assert entity.major_axis == [0, 1]
    assert entity.start_angle == 0.0
    assert entity.end_angle == math.pi


def test_spline_emits_one_point_per_defining_point():
    sketch = NxSketch(name="S")
    sketch.curves.append(
        NxCurve(id=1, kind=NxCurveKind.SPLINE,
                spline_points=[[0, 0], [10, 20], [30, 0]], closed=False, fit=True)
    )
    data = _translate(sketch)
    assert len(data.points) == 3
    entity = data.entities[0]
    assert entity.kind == "spline"
    assert entity.points == [p.id for p in data.points]
    assert entity.fit is True
    assert entity.closed is None  # omitted when False


def test_closed_spline_sets_closed_flag():
    sketch = NxSketch(name="S")
    sketch.curves.append(
        NxCurve(id=1, kind=NxCurveKind.SPLINE, spline_points=[[0, 0], [5, 5], [10, 0]], closed=True)
    )
    assert _translate(sketch).entities[0].closed is True


def test_symmetry_constraint_two_points_about_a_line():
    sketch = NxSketch(name="Sym")
    sketch.curves.append(NxCurve(id=1, kind=NxCurveKind.LINE, start=[0, -5], end=[0, 5]))  # axis
    sketch.curves.append(NxCurve(id=2, kind=NxCurveKind.LINE, start=[-3, 0], end=[3, 0]))  # spanned
    con = NxSketchConstraint(kind=NxConstraintKind.SYMMETRY)
    con.points.append(NxPointRef(2, NxCurvePointRole.START))
    con.points.append(NxPointRef(2, NxCurvePointRole.END))
    con.curves.append(1)
    sketch.constraints.append(con)
    row = _translate(sketch).constraints[0]
    assert row.kind == "symmetry"
    assert len(row.points) == 2 and len(row.curves) == 1


def test_ground_constraint_lists_all_points():
    sketch = NxSketch(name="G")
    sketch.curves.append(NxCurve(id=1, kind=NxCurveKind.LINE, start=[0, 0], end=[10, 0]))
    con = NxSketchConstraint(kind=NxConstraintKind.GROUND)
    con.points.append(NxPointRef(1, NxCurvePointRole.START))
    con.points.append(NxPointRef(1, NxCurvePointRole.END))
    sketch.constraints.append(con)
    row = _translate(sketch).constraints[0]
    assert row.kind == "ground"
    assert len(row.points) == 2 and row.curves == []


def test_smooth_constraint_two_points_two_curves():
    sketch = NxSketch(name="Sm")
    sketch.curves.append(NxCurve(id=1, kind=NxCurveKind.LINE, start=[0, 0], end=[10, 0]))
    sketch.curves.append(
        NxCurve(id=2, kind=NxCurveKind.SPLINE, spline_points=[[10, 0], [20, 5], [30, 0]])
    )
    con = NxSketchConstraint(kind=NxConstraintKind.SMOOTH)
    con.points.append(NxPointRef(1, NxCurvePointRole.END))
    con.points.append(NxPointRef(2, NxCurvePointRole.START))
    con.curves.extend([1, 2])
    # Spline endpoint refs aren't role-addressable, so point at the line ends for the test:
    con.points[1] = NxPointRef(1, NxCurvePointRole.START)
    sketch.constraints.append(con)
    row = _translate(sketch).constraints[0]
    assert row.kind == "smooth"
    assert len(row.points) == 2 and len(row.curves) == 2
