# SPDX-License-Identifier: GPL-2.0-only
"""Unit tests for the pure IR -> recipe translators."""
import math

from oblikovati_exporter_nx.model.document import NxExpression
from oblikovati_exporter_nx.model.dressup import (
    NxChamfer,
    NxDraft,
    NxEdgeDescriptor,
    NxFaceDescriptor,
    NxFillet,
    NxHole,
    NxShell,
)
from oblikovati_exporter_nx.model.feature import (
    NxCircularPattern,
    NxExtentDirection,
    NxExtrude,
    NxMirror,
    NxOperation,
    NxRectangularPattern,
    NxRevolve,
)
from oblikovati_exporter_nx.model.sketch import (
    NxConstraintKind,
    NxCurve,
    NxCurveKind,
    NxCurvePointRole,
    NxDimensionKind,
    NxPointRef,
    NxSketch,
    NxSketchConstraint,
    NxSketchDimension,
)
from oblikovati_exporter_nx.model.workfeature import NxWorkPlane
from oblikovati_exporter_nx.translate import (
    parameter_translator,
    workplane_translator,
)
from oblikovati_exporter_nx.translate.feature_translator import FeatureTranslator
from oblikovati_exporter_nx.translate.id_allocator import IdAllocator
from oblikovati_exporter_nx.translate.report import ExportReport
from oblikovati_exporter_nx.translate.sketch_translator import SketchTranslator


def test_numeric_parameter_gets_inline_unit():
    row = parameter_translator.translate(NxExpression(name="w", formula="40", unit="mm"))
    assert row.name == "w"
    assert row.kind == "user"
    assert row.expression == "40 mm"


def test_formula_parameter_passes_through_without_unit():
    row = parameter_translator.translate(NxExpression(name="t", formula="width * 2", unit="mm"))
    assert row.expression == "width * 2"


def test_workplane_scales_origin_to_centimetres():
    row = workplane_translator.translate(NxWorkPlane(name="d", origin=[0, 0, 10]))
    assert row.collection == "plane"
    assert row.kind == "fixed-frame"
    assert row.position == [0.0, 0.0, 1.0]


def test_extrude_converts_units_and_direction():
    feature = NxExtrude(
        name="E", sketch_index=2, profile_index=0, distance=50,
        direction=NxExtentDirection.POSITIVE, operation=NxOperation.JOIN,
    )
    data = FeatureTranslator(ExportReport()).translate(feature, {})
    assert data.kind == "extrude"
    assert data.payload.sketch == 2
    assert data.payload.profiles == [0]
    assert data.payload.operation == "join"
    assert data.payload.distance == 5.0
    assert data.payload.extent == "distance"


def test_revolve_full_turn_leaves_angle_unset():
    feature = NxRevolve(name="R", sketch_index=0, angle_degrees=0)
    data = FeatureTranslator(ExportReport()).translate(feature, {})
    assert data.kind == "revolve"
    assert data.payload.angle is None


def test_circular_pattern_full_turn_is_two_pi():
    feature = NxCircularPattern(name="P", count=4, angle_degrees=0)
    feature.source_feature_indices.append(0)
    data = FeatureTranslator(ExportReport()).translate(feature, {0: 0})
    assert data.kind == "circular-pattern"
    assert data.payload.count == 4
    assert data.payload.angle == 2 * math.pi


def test_pattern_with_unresolved_source_is_skipped_and_reported():
    report = ExportReport()
    feature = NxRectangularPattern(name="P", count_x=2)
    feature.source_feature_indices.append(7)  # never translated
    assert FeatureTranslator(report).translate(feature, {}) is None
    assert report.has_warnings


def test_mirror_remaps_source_program_index():
    feature = NxMirror(name="M", plane_origin=[0, 0, 0], plane_normal=[1, 0, 0])
    feature.source_feature_indices.append(3)
    data = FeatureTranslator(ExportReport()).translate(feature, {3: 1})
    assert data.payload.source == [1]


def test_unknown_feature_is_reported():
    report = ExportReport()
    from oblikovati_exporter_nx.model.feature import NxFeature

    assert FeatureTranslator(report).translate(NxFeature(name="X"), {}) is None
    assert report.has_warnings


def test_fillet_carries_geometric_edge_in_centimetres():
    feature = NxFillet(name="F", radius_mm=5)
    feature.edges.append(NxEdgeDescriptor(midpoint=[20, 0, 50], direction=[1, 0, 0]))
    data = FeatureTranslator(ExportReport()).translate(feature, {})
    assert data.kind == "fillet"
    assert data.payload.value == 0.5
    assert data.payload.geom_edges[0].midpoint == [2.0, 0.0, 5.0]


def test_chamfer_shell_draft_hole_dispatch():
    report = ExportReport()
    tr = FeatureTranslator(report)
    chamfer = NxChamfer(name="C", distance_mm=5)
    chamfer.edges.append(NxEdgeDescriptor(midpoint=[0, 0, 0], direction=[1, 0, 0]))
    shell = NxShell(name="S", thickness_mm=5)
    shell.removed_faces.append(NxFaceDescriptor(centroid=[0, 0, 0]))
    draft = NxDraft(name="D", angle_degrees=90)
    draft.faces.append(NxFaceDescriptor(centroid=[0, 0, 0]))
    hole = NxHole(name="H", diameter_mm=10, depth_mm=20,
                  placement_face=NxFaceDescriptor(centroid=[0, 0, 0]))
    assert tr.translate(chamfer, {}).kind == "chamfer"
    assert tr.translate(shell, {}).kind == "shell"
    assert abs(tr.translate(draft, {}).payload.value - math.pi / 2) < 1e-12
    hole_data = tr.translate(hole, {})
    assert hole_data.payload.diameter == 1.0
    assert hole_data.payload.depth == 2.0
    assert hole_data.payload.center is None  # centroid drill: center omitted


def test_hole_with_explicit_center_scaled_to_centimetres():
    hole = NxHole(
        name="H", diameter_mm=10, depth_mm=20,
        placement_face=NxFaceDescriptor(centroid=[20, 15, 50]),
        center=[10, 10, 50],
    )
    payload = FeatureTranslator(ExportReport()).translate(hole, {}).payload
    assert payload.center == [1.0, 1.0, 5.0]  # mm -> cm


def _square_sketch():
    sketch = NxSketch(name="Sq")
    for cid, (x0, y0, x1, y1) in enumerate(
        [(0, 0, 40, 0), (40, 0, 40, 30), (40, 30, 0, 30), (0, 30, 0, 0)], start=1
    ):
        sketch.curves.append(NxCurve(id=cid, kind=NxCurveKind.LINE, start=[x0, y0], end=[x1, y1]))
    return sketch


def test_sketch_translates_points_entities_constraints_dimensions():
    sketch = _square_sketch()
    coincident = NxSketchConstraint(kind=NxConstraintKind.COINCIDENT)
    coincident.points.append(NxPointRef(1, NxCurvePointRole.END))
    coincident.points.append(NxPointRef(2, NxCurvePointRole.START))
    sketch.constraints.append(coincident)
    horizontal = NxSketchConstraint(kind=NxConstraintKind.HORIZONTAL)
    horizontal.curves.append(1)
    sketch.constraints.append(horizontal)
    dim = NxSketchDimension(kind=NxDimensionKind.DISTANCE, expression="width")
    dim.points.append(NxPointRef(1, NxCurvePointRole.START))
    dim.points.append(NxPointRef(1, NxCurvePointRole.END))
    sketch.dimensions.append(dim)

    ids = IdAllocator()
    sketch_id = ids.next()
    data = SketchTranslator(ids, ExportReport()).translate(sketch, sketch_id)

    assert data.id == sketch_id
    assert len(data.points) == 8  # 4 lines x 2 endpoints
    assert [e.kind for e in data.entities] == ["line"] * 4
    assert data.constraints[0].kind == "coincident"
    assert len(data.constraints[0].points) == 2
    assert data.constraints[1].kind == "horizontal"  # line -> two endpoint ids
    assert len(data.constraints[1].points) == 2
    assert data.dimensions[0].kind == "distance"
    assert data.dimensions[0].expression == "width"


def test_circle_dimension_references_curve_not_points():
    sketch = NxSketch(name="C")
    sketch.curves.append(NxCurve(id=1, kind=NxCurveKind.CIRCLE, center=[0, 0], radius=20))
    dim = NxSketchDimension(kind=NxDimensionKind.DIAMETER, expression="dia")
    dim.curves.append(1)
    sketch.dimensions.append(dim)

    ids = IdAllocator()
    data = SketchTranslator(ids, ExportReport()).translate(sketch, ids.next())
    assert data.entities[0].kind == "circle"
    assert data.entities[0].radius == 2.0
    assert data.dimensions[0].curves == [data.entities[0].id]
    assert data.dimensions[0].points == []
