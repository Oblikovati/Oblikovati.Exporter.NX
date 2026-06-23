# SPDX-License-Identifier: GPL-2.0-only
"""NX-neutral sample documents used both by the unit tests and the golden generator.

Mirrors NxSampleParts.cs so the Python and C# exporters are exercised on identical inputs
and produce identical output. Coordinates are in millimetres (the IR contract).
"""
from __future__ import annotations

from typing import List

from ..model.document import NxDocument, NxDocumentKind, NxExpression, NxOccurrence
from ..model.dressup import (
    NxChamfer,
    NxEdgeDescriptor,
    NxFaceDescriptor,
    NxFillet,
    NxHole,
    NxShell,
)
from ..model.feature import (
    NxCircularPattern,
    NxExtentDirection,
    NxExtrude,
    NxLoft,
    NxLoftSection,
    NxMirror,
    NxOperation,
    NxRectangularPattern,
    NxRevolve,
    NxSweep,
)
from ..model.sketch import (
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


def empty_part() -> NxDocument:
    return NxDocument(display_name="empty-part", kind=NxDocumentKind.PART)


def parametric_part() -> NxDocument:
    part = NxDocument(display_name="parametric-part", kind=NxDocumentKind.PART, length_unit="mm")
    part.expressions.append(NxExpression(name="width", formula="40", unit="mm"))
    part.expressions.append(NxExpression(name="twice", formula="width * 2", unit="mm"))
    return part


def rectangle_part() -> NxDocument:
    """A 40x30 mm rectangle, fully constrained (DOF 0)."""
    part = NxDocument(display_name="rectangle-part", kind=NxDocumentKind.PART, length_unit="mm")
    part.expressions.append(NxExpression(name="width", formula="40", unit="mm"))
    part.expressions.append(NxExpression(name="height", formula="30", unit="mm"))

    sketch = NxSketch(name="Rectangle")
    l0, l1, l2, l3 = 1, 2, 3, 4
    sketch.curves.append(_line(l0, 0, 0, 40, 0))   # bottom
    sketch.curves.append(_line(l1, 40, 0, 40, 30))  # right
    sketch.curves.append(_line(l2, 40, 30, 0, 30))  # top
    sketch.curves.append(_line(l3, 0, 30, 0, 0))   # left

    _coincide(sketch, l0, NxCurvePointRole.END, l1, NxCurvePointRole.START)
    _coincide(sketch, l1, NxCurvePointRole.END, l2, NxCurvePointRole.START)
    _coincide(sketch, l2, NxCurvePointRole.END, l3, NxCurvePointRole.START)
    _coincide(sketch, l3, NxCurvePointRole.END, l0, NxCurvePointRole.START)

    sketch.constraints.append(_on_curves(NxConstraintKind.HORIZONTAL, l0))
    sketch.constraints.append(_on_curves(NxConstraintKind.HORIZONTAL, l2))
    sketch.constraints.append(_on_curves(NxConstraintKind.VERTICAL, l1))
    sketch.constraints.append(_on_curves(NxConstraintKind.VERTICAL, l3))
    sketch.constraints.append(_fix(l0, NxCurvePointRole.START))

    sketch.dimensions.append(_distance(l0, NxCurvePointRole.START, l0, NxCurvePointRole.END, "width"))
    sketch.dimensions.append(_distance(l3, NxCurvePointRole.START, l3, NxCurvePointRole.END, "height"))

    part.sketches.append(sketch)
    return part


def box_part() -> NxDocument:
    """The rectangle extruded 50 mm into a 40x30x50 mm box (60 cm^3)."""
    part = rectangle_part()
    part.display_name = "box-part"
    part.features.append(
        NxExtrude(
            name="Extrude1",
            sketch_index=0,
            profile_index=0,
            operation=NxOperation.NEW_BODY,
            direction=NxExtentDirection.POSITIVE,
            distance=50,
        )
    )
    return part


def revolve_part() -> NxDocument:
    """An offset square revolved full about a vertical centerline — a washer (24*pi cm^3)."""
    part = NxDocument(display_name="revolve-part", kind=NxDocumentKind.PART, length_unit="mm")
    part.expressions.append(NxExpression(name="side", formula="20", unit="mm"))

    sketch = NxSketch(name="Section")
    l0, l1, l2, l3, axis = 1, 2, 3, 4, 5
    sketch.curves.append(_line(l0, 20, 0, 40, 0))   # bottom
    sketch.curves.append(_line(l1, 40, 0, 40, 20))  # outer
    sketch.curves.append(_line(l2, 40, 20, 20, 20))  # top
    sketch.curves.append(_line(l3, 20, 20, 20, 0))  # inner
    centerline = _line(axis, 0, 0, 0, 20)
    centerline.centerline = True
    sketch.curves.append(centerline)

    _coincide(sketch, l0, NxCurvePointRole.END, l1, NxCurvePointRole.START)
    _coincide(sketch, l1, NxCurvePointRole.END, l2, NxCurvePointRole.START)
    _coincide(sketch, l2, NxCurvePointRole.END, l3, NxCurvePointRole.START)
    _coincide(sketch, l3, NxCurvePointRole.END, l0, NxCurvePointRole.START)

    sketch.constraints.append(_on_curves(NxConstraintKind.HORIZONTAL, l0))
    sketch.constraints.append(_on_curves(NxConstraintKind.HORIZONTAL, l2))
    sketch.constraints.append(_on_curves(NxConstraintKind.VERTICAL, l1))
    sketch.constraints.append(_on_curves(NxConstraintKind.VERTICAL, l3))
    sketch.constraints.append(_fix(l0, NxCurvePointRole.START))
    sketch.dimensions.append(_distance(l0, NxCurvePointRole.START, l0, NxCurvePointRole.END, "side"))
    sketch.dimensions.append(_distance(l3, NxCurvePointRole.START, l3, NxCurvePointRole.END, "side"))

    # Pin the centerline (vertical on the Y axis, length "side").
    sketch.constraints.append(_on_curves(NxConstraintKind.VERTICAL, axis))
    sketch.constraints.append(_fix(axis, NxCurvePointRole.START))
    sketch.dimensions.append(_distance(axis, NxCurvePointRole.START, axis, NxCurvePointRole.END, "side"))

    part.sketches.append(sketch)
    part.features.append(
        NxRevolve(
            name="Revolve1",
            sketch_index=0,
            profile_index=0,
            operation=NxOperation.NEW_BODY,
            angle_degrees=0,  # full revolution
        )
    )
    return part


def circle_part() -> NxDocument:
    """A circle fixed at the origin with a diameter dimension (DOF 0)."""
    part = NxDocument(display_name="circle-part", kind=NxDocumentKind.PART, length_unit="mm")
    part.expressions.append(NxExpression(name="dia", formula="40", unit="mm"))

    sketch = NxSketch(name="Circle")
    c0 = 1
    sketch.curves.append(NxCurve(id=c0, kind=NxCurveKind.CIRCLE, center=[0, 0], radius=20))
    sketch.constraints.append(_fix(c0, NxCurvePointRole.CENTER))
    diameter = NxSketchDimension(kind=NxDimensionKind.DIAMETER, expression="dia")
    diameter.curves.append(c0)
    sketch.dimensions.append(diameter)

    part.sketches.append(sketch)
    return part


def rect_pattern_part() -> NxDocument:
    """Box (60 cm^3) replicated 1x3 along +X (180 cm^3)."""
    part = _make_box("rect-pattern-part", 0)
    pattern = NxRectangularPattern(
        name="Pattern1", count_x=3, count_y=1, step_x=[60, 0, 0], step_y=[0, 0, 0]
    )
    pattern.source_feature_indices.append(0)  # the extrude
    part.features.append(pattern)
    return part


def mirror_part() -> NxDocument:
    """Box mirrored across the YZ plane (x = 0): 120 cm^3."""
    part = _make_box("mirror-part", 0)
    mirror = NxMirror(name="Mirror1", plane_origin=[0, 0, 0], plane_normal=[1, 0, 0])
    mirror.source_feature_indices.append(0)
    part.features.append(mirror)
    return part


def circular_pattern_part() -> NxDocument:
    """Box offset 100 mm from the Z axis, circular-patterned 4x full turn (240 cm^3)."""
    part = _make_box("circular-pattern-part", 100)
    pattern = NxCircularPattern(
        name="Pattern1", count=4, angle_degrees=0, axis_point=[0, 0, 0], axis_dir=[0, 0, 1]
    )
    pattern.source_feature_indices.append(0)
    part.features.append(pattern)
    return part


def filleted_box_part() -> NxDocument:
    """The 60 cm^3 box with a 5 mm fillet on a top edge (geometric ref, ADR-0040)."""
    part = _make_box("filleted-box-part", 0)
    fillet = NxFillet(name="Fillet1", radius_mm=5)
    fillet.edges.append(NxEdgeDescriptor(midpoint=[20, 0, 50], direction=[1, 0, 0]))
    part.features.append(fillet)
    return part


def chamfered_box_part() -> NxDocument:
    """The 60 cm^3 box with a 5 mm chamfer on a top edge (geometric ref)."""
    part = _make_box("chamfered-box-part", 0)
    chamfer = NxChamfer(name="Chamfer1", distance_mm=5)
    chamfer.edges.append(NxEdgeDescriptor(midpoint=[20, 0, 50], direction=[1, 0, 0]))
    part.features.append(chamfer)
    return part


def shelled_box_part() -> NxDocument:
    """The box shelled to a 5 mm wall, removing the top face (geometric ref)."""
    part = _make_box("shelled-box-part", 0)
    shell = NxShell(name="Shell1", thickness_mm=5)
    shell.removed_faces.append(NxFaceDescriptor(centroid=[20, 15, 50], normal=[0, 0, 1]))
    part.features.append(shell)
    return part


def holed_box_part() -> NxDocument:
    """The 60 cm^3 box with a 10 mm drilled hole 20 mm deep into its top face."""
    part = _make_box("holed-box-part", 0)
    hole = NxHole(
        name="Hole1",
        diameter_mm=10,
        depth_mm=20,
        placement_face=NxFaceDescriptor(centroid=[20, 15, 50], normal=[0, 0, 1]),
    )
    part.features.append(hole)
    return part


def offset_holed_box_part() -> NxDocument:
    """The 60 cm^3 box with a Ø10 mm hole 20 mm deep drilled at an explicit OFF-CENTRE
    point (10, 10, 50) mm on the top face — not the face centroid (~58.43 cm^3)."""
    part = _make_box("offset-holed-box-part", 0)
    hole = NxHole(
        name="Hole1",
        diameter_mm=10,
        depth_mm=20,
        placement_face=NxFaceDescriptor(centroid=[20, 15, 50], normal=[0, 0, 1]),
        center=[10, 10, 50],
    )
    part.features.append(hole)
    return part


def assembly_doc() -> NxDocument:
    """An assembly placing the same 60 cm^3 box twice (origin and x = 100 mm)."""
    box = _make_box("box-component", 0)
    assembly = NxDocument(display_name="assembly", kind=NxDocumentKind.ASSEMBLY, length_unit="mm")
    assembly.occurrences.append(NxOccurrence(name="box-component:1", component=box))
    assembly.occurrences.append(
        NxOccurrence(name="box-component:2", component=box, position=[100, 0, 0])
    )
    return assembly


def sweep_part() -> NxDocument:
    """A circle profile (Ø10 mm) swept 50 mm along +Z — a cylinder (~3.93 cm^3)."""
    part = NxDocument(display_name="sweep-part", kind=NxDocumentKind.PART, length_unit="mm")
    part.expressions.append(NxExpression(name="dia", formula="10", unit="mm"))
    sketch = NxSketch(name="Profile")
    sketch.curves.append(NxCurve(id=1, kind=NxCurveKind.CIRCLE, center=[0, 0], radius=5))
    sketch.constraints.append(_fix(1, NxCurvePointRole.CENTER))
    diameter = NxSketchDimension(kind=NxDimensionKind.DIAMETER, expression="dia")
    diameter.curves.append(1)
    sketch.dimensions.append(diameter)
    part.sketches.append(sketch)
    part.features.append(
        NxSweep(
            name="Sweep1", profile_sketch_index=0, profile_index=0,
            path=[[0, 0, 0], [0, 0, 50]], operation=NxOperation.NEW_BODY,
        )
    )
    return part


def loft_part() -> NxDocument:
    """A loft between a Ø10 mm circle at z=0 and a Ø20 mm circle at z=50 mm (a frustum)."""
    part = NxDocument(display_name="loft-part", kind=NxDocumentKind.PART, length_unit="mm")
    part.expressions.append(NxExpression(name="d0", formula="10", unit="mm"))
    part.expressions.append(NxExpression(name="d1", formula="20", unit="mm"))

    bottom = NxSketch(name="Bottom")
    bottom.curves.append(NxCurve(id=1, kind=NxCurveKind.CIRCLE, center=[0, 0], radius=5))
    bottom.constraints.append(_fix(1, NxCurvePointRole.CENTER))
    dim0 = NxSketchDimension(kind=NxDimensionKind.DIAMETER, expression="d0")
    dim0.curves.append(1)
    bottom.dimensions.append(dim0)

    top = NxSketch(name="Top", origin=[0, 0, 50])
    top.curves.append(NxCurve(id=1, kind=NxCurveKind.CIRCLE, center=[0, 0], radius=10))
    top.constraints.append(_fix(1, NxCurvePointRole.CENTER))
    dim1 = NxSketchDimension(kind=NxDimensionKind.DIAMETER, expression="d1")
    dim1.curves.append(1)
    top.dimensions.append(dim1)

    part.sketches.extend([bottom, top])
    part.features.append(
        NxLoft(
            name="Loft1",
            sections=[NxLoftSection(0, 0), NxLoftSection(1, 0)],
            operation=NxOperation.NEW_BODY,
        )
    )
    return part


def arc_slot_part() -> NxDocument:
    """A rounded slot: two horizontal lines closed by two semicircular arcs.

    Exercises the arc entity (center/start/end + ccw) inside a closed profile. Round-trip
    is OPEN_ONLY (free-form arc DOF=0 is impractical to hand-author); field exactness is
    covered by the translator unit tests.
    """
    part = NxDocument(display_name="arc-slot-part", kind=NxDocumentKind.PART, length_unit="mm")
    sketch = NxSketch(name="Slot")
    top, right, bottom, left = 1, 2, 3, 4
    sketch.curves.append(_line(top, -20, 10, 20, 10))   # top edge
    sketch.curves.append(NxCurve(  # right cap, from (20,10) down to (20,-10)
        id=right, kind=NxCurveKind.ARC, center=[20, 0], start=[20, 10], end=[20, -10], ccw=False))
    sketch.curves.append(_line(bottom, 20, -10, -20, -10))  # bottom edge
    sketch.curves.append(NxCurve(  # left cap, from (-20,-10) up to (-20,10)
        id=left, kind=NxCurveKind.ARC, center=[-20, 0], start=[-20, -10], end=[-20, 10], ccw=False))
    _coincide(sketch, top, NxCurvePointRole.END, right, NxCurvePointRole.START)
    _coincide(sketch, right, NxCurvePointRole.END, bottom, NxCurvePointRole.START)
    _coincide(sketch, bottom, NxCurvePointRole.END, left, NxCurvePointRole.START)
    _coincide(sketch, left, NxCurvePointRole.END, top, NxCurvePointRole.START)
    part.sketches.append(sketch)
    return part


def ellipse_part() -> NxDocument:
    """A single ellipse (major axis +X, 40x20 mm) fixed at the origin. OPEN_ONLY round-trip."""
    part = NxDocument(display_name="ellipse-part", kind=NxDocumentKind.PART, length_unit="mm")
    sketch = NxSketch(name="Ellipse")
    sketch.curves.append(
        NxCurve(id=1, kind=NxCurveKind.ELLIPSE, center=[0, 0],
                major_axis=[1, 0], major_radius=40, minor_radius=20)
    )
    sketch.constraints.append(_fix(1, NxCurvePointRole.CENTER))
    part.sketches.append(sketch)
    return part


def spline_part() -> NxDocument:
    """A through-points (fit) spline of four points. OPEN_ONLY round-trip."""
    part = NxDocument(display_name="spline-part", kind=NxDocumentKind.PART, length_unit="mm")
    sketch = NxSketch(name="Spline")
    sketch.curves.append(
        NxCurve(id=1, kind=NxCurveKind.SPLINE,
                spline_points=[[0, 0], [20, 30], [50, 10], [70, 40]], fit=True)
    )
    part.sketches.append(sketch)
    return part


def datum_plane_part() -> NxDocument:
    """A part carrying one datum plane offset 10 mm above XY (a fixed frame)."""
    part = NxDocument(display_name="datum-plane-part", kind=NxDocumentKind.PART, length_unit="mm")
    from ..model.workfeature import NxWorkPlane

    part.work_planes.append(
        NxWorkPlane(name="Datum1", origin=[0, 0, 10], xaxis=[1, 0, 0], yaxis=[0, 1, 0])
    )
    return part


def all_fixtures() -> List[NxDocument]:
    """Every fixture, in the same order the C# golden generator emits them."""
    return [
        empty_part(),
        parametric_part(),
        rectangle_part(),
        circle_part(),
        box_part(),
        revolve_part(),
        datum_plane_part(),
        rect_pattern_part(),
        mirror_part(),
        circular_pattern_part(),
        assembly_doc(),
        filleted_box_part(),
        chamfered_box_part(),
        shelled_box_part(),
        holed_box_part(),
        offset_holed_box_part(),
        sweep_part(),
        loft_part(),
        arc_slot_part(),
        ellipse_part(),
        spline_part(),
    ]


# Fixtures whose round-trip is open-only (the file must load/recompute in the real reader,
# but free-form arc/ellipse/spline sketches aren't hand-authored to DOF 0). Field-level
# correctness for these is asserted by the translator unit tests instead.
OPEN_ONLY = frozenset({"arc-slot-part.opd", "ellipse-part.opd", "spline-part.opd"})


def _make_box(name: str, x0: float) -> NxDocument:
    """A fully-constrained 40x30 mm rectangle at (x0, 0), extruded 50 mm."""
    part = NxDocument(display_name=name, kind=NxDocumentKind.PART, length_unit="mm")
    part.expressions.append(NxExpression(name="bw", formula="40", unit="mm"))
    part.expressions.append(NxExpression(name="bh", formula="30", unit="mm"))

    sketch = NxSketch(name="Base")
    l0, l1, l2, l3 = 1, 2, 3, 4
    sketch.curves.append(_line(l0, x0, 0, x0 + 40, 0))
    sketch.curves.append(_line(l1, x0 + 40, 0, x0 + 40, 30))
    sketch.curves.append(_line(l2, x0 + 40, 30, x0, 30))
    sketch.curves.append(_line(l3, x0, 30, x0, 0))
    _coincide(sketch, l0, NxCurvePointRole.END, l1, NxCurvePointRole.START)
    _coincide(sketch, l1, NxCurvePointRole.END, l2, NxCurvePointRole.START)
    _coincide(sketch, l2, NxCurvePointRole.END, l3, NxCurvePointRole.START)
    _coincide(sketch, l3, NxCurvePointRole.END, l0, NxCurvePointRole.START)
    sketch.constraints.append(_on_curves(NxConstraintKind.HORIZONTAL, l0))
    sketch.constraints.append(_on_curves(NxConstraintKind.HORIZONTAL, l2))
    sketch.constraints.append(_on_curves(NxConstraintKind.VERTICAL, l1))
    sketch.constraints.append(_on_curves(NxConstraintKind.VERTICAL, l3))
    sketch.constraints.append(_fix(l0, NxCurvePointRole.START))
    sketch.dimensions.append(_distance(l0, NxCurvePointRole.START, l0, NxCurvePointRole.END, "bw"))
    sketch.dimensions.append(_distance(l3, NxCurvePointRole.START, l3, NxCurvePointRole.END, "bh"))
    part.sketches.append(sketch)

    part.features.append(
        NxExtrude(
            name="Extrude1",
            sketch_index=0,
            profile_index=0,
            operation=NxOperation.NEW_BODY,
            direction=NxExtentDirection.POSITIVE,
            distance=50,
        )
    )
    return part


def _line(curve_id: int, x0: float, y0: float, x1: float, y1: float) -> NxCurve:
    return NxCurve(id=curve_id, kind=NxCurveKind.LINE, start=[x0, y0], end=[x1, y1])


def _coincide(sketch, ca, ra, cb, rb) -> None:
    constraint = NxSketchConstraint(kind=NxConstraintKind.COINCIDENT)
    constraint.points.append(NxPointRef(ca, ra))
    constraint.points.append(NxPointRef(cb, rb))
    sketch.constraints.append(constraint)


def _on_curves(kind, *curves) -> NxSketchConstraint:
    constraint = NxSketchConstraint(kind=kind)
    for curve_id in curves:
        constraint.curves.append(curve_id)
    return constraint


def _fix(curve_id, role) -> NxSketchConstraint:
    constraint = NxSketchConstraint(kind=NxConstraintKind.FIX)
    constraint.points.append(NxPointRef(curve_id, role))
    return constraint


def _distance(ca, ra, cb, rb, expr) -> NxSketchDimension:
    dimension = NxSketchDimension(kind=NxDimensionKind.DISTANCE, expression=expr)
    dimension.points.append(NxPointRef(ca, ra))
    dimension.points.append(NxPointRef(cb, rb))
    return dimension
