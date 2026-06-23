# SPDX-License-Identifier: GPL-2.0-only
"""Reads a part's feature history into IR features.

Dispatches on the NX feature type and reads each feature through its builder (the
documented way to read an existing feature): scalar parameters from the feature's
expressions/builder, selected geometry from the builder's collectors -> the geometric
descriptors the dress-ups carry (ADR-0040). A sketch-based feature resolves its section
to the IR sketch index via the curve-tag -> sketch map built during sketch extraction.

UNVERIFIED — needs a real NX session; builder member shapes follow the documented NXOpen
API. Builders are always Destroyed (never Committed), so reading does not alter the part.

Done: extrude, revolve, fillet, chamfer, shell, draft, hole, pattern, mirror. Deferred
(need feature-specific APIs / live NX): partial-arc/spline sketch geometry. Profile index
defaults to 0 (NX section -> Oblikovati region index has no stable mapping).
"""
from __future__ import annotations

import math
from typing import Dict, List, Optional

import NXOpen

from ..model.dressup import NxChamfer, NxDraft, NxFillet, NxHole, NxShell
from ..model.feature import (
    NxCircularPattern,
    NxExtentDirection,
    NxExtrude,
    NxFeature,
    NxLoft,
    NxLoftSection,
    NxMirror,
    NxOperation,
    NxRectangularPattern,
    NxRevolve,
    NxSweep,
)
from . import centerline_injector, edge_geometry, face_geometry, sweep_path

_RAD_TO_DEG = 180.0 / math.pi


def extract(part, document, curve_tag_to_sketch: Dict[int, int]) -> None:
    # Map each NX feature tag to its IR index as it is added, so a pattern/mirror can
    # resolve the source features it replicates (which always come earlier).
    feature_tag_to_index: Dict[int, int] = {}
    for feature in part.Features:
        extracted = _extract_feature(
            part, feature, document, curve_tag_to_sketch, feature_tag_to_index
        )
        if extracted is not None:
            feature_tag_to_index[feature.Tag] = len(document.features)
            document.features.append(extracted)


def _extract_feature(
    part, feature, document, curve_tag_to_sketch, feature_tag_to_index
) -> Optional[NxFeature]:
    feature_type = feature.FeatureType
    if feature_type == "EXTRUDE":
        return _extrude(part, feature, curve_tag_to_sketch)
    if feature_type in ("REVOLVE", "REVOLVED"):
        return _revolve(part, feature, document, curve_tag_to_sketch)
    if feature_type in ("SWEEP", "SWEPT", "VARIATIONAL SWEEP", "SWEEP ALONG GUIDE"):
        return _sweep(part, feature, curve_tag_to_sketch)
    if feature_type in ("THROUGH CURVES", "THROUGH_CURVES", "TCRV", "LOFT"):
        return _loft(part, feature, curve_tag_to_sketch)
    if feature_type == "EDGE BLEND":
        return _fillet(part, feature)
    if feature_type == "CHAMFER":
        return _chamfer(part, feature)
    if feature_type in ("HOLLOW", "SHELL"):
        return _shell(part, feature)
    if feature_type == "DRAFT":
        return _draft(part, feature)
    if feature_type in ("SIMPLE HOLE", "HOLE PACKAGE", "HOLE"):
        return _hole(part, feature)
    if feature_type == "PATTERN FEATURE":
        return _pattern(part, feature, feature_tag_to_index)
    if feature_type in ("MIRROR FEATURE", "MIRROR"):
        return _mirror(part, feature, feature_tag_to_index)
    return None  # exotic geometry / sketch constraints: live-NX completion


def _extrude(part, feature, curve_tag_to_sketch) -> Optional[NxFeature]:
    builder = part.Features.CreateExtrudeBuilder(feature)
    try:
        sketch = _sketch_index_of(builder.Section, curve_tag_to_sketch)
        if sketch < 0:
            return None
        start = builder.Limits.StartExtend.Value.Value
        end = builder.Limits.EndExtend.Value.Value
        return NxExtrude(
            name=feature.Name,
            sketch_index=sketch,
            profile_index=0,
            operation=NxOperation.NEW_BODY,
            distance=end,
            second_distance=abs(start) if start != 0 else 0,
            direction=NxExtentDirection.SYMMETRIC if start != 0 else NxExtentDirection.POSITIVE,
        )
    finally:
        builder.Destroy()


def _revolve(part, feature, document, curve_tag_to_sketch) -> Optional[NxFeature]:
    builder = part.Features.CreateRevolveBuilder(feature)
    try:
        sketch = _sketch_index_of(builder.Section, curve_tag_to_sketch)
        if sketch < 0:
            return None
        angle = builder.Limits.EndExtend.Value.Value - builder.Limits.StartExtend.Value.Value
        centerline_injector.inject(document.sketches[sketch], builder.Axis)
        full = abs(angle - 2 * math.pi) < 1e-6
        return NxRevolve(
            name=feature.Name,
            sketch_index=sketch,
            profile_index=0,
            operation=NxOperation.NEW_BODY,
            angle_degrees=0 if full else angle * _RAD_TO_DEG,
        )
    finally:
        builder.Destroy()


def _sweep(part, feature, curve_tag_to_sketch) -> Optional[NxFeature]:
    # NX swept feature: the first section is the profile; the guide string is the path.
    # The path is tessellated to a 3D polyline (Oblikovati stores points, not a sketch).
    builder = part.Features.CreateSweptBuilder(feature)
    try:
        sections = _sections_of(builder, ("SectionList", "Sections"))
        guides = _sections_of(builder, ("GuideList", "Guides"))
        if not sections or not guides:
            return None
        profile_sketch = _sketch_index_of(sections[0], curve_tag_to_sketch)
        path = sweep_path.polyline(guides[0])
        if profile_sketch < 0 or len(path) < 2:
            return None
        return NxSweep(
            name=feature.Name, profile_sketch_index=profile_sketch, profile_index=0,
            path=path, operation=NxOperation.NEW_BODY,
        )
    finally:
        builder.Destroy()


def _loft(part, feature, curve_tag_to_sketch) -> Optional[NxFeature]:
    # NX "through curves": each section is a profile sketch; loft runs through them in order.
    builder = part.Features.CreateThroughCurvesBuilder(feature)
    try:
        sections = _sections_of(builder, ("SectionsList", "SectionList", "Sections"))
        loft = NxLoft(name=feature.Name, operation=NxOperation.NEW_BODY)
        for section in sections:
            index = _sketch_index_of(section, curve_tag_to_sketch)
            if index < 0:
                return None  # a section did not resolve to an extracted sketch
            loft.sections.append(NxLoftSection(sketch_index=index, profile_index=0))
        return loft if len(loft.sections) >= 2 else None
    finally:
        builder.Destroy()


# A swept/through-curves builder keeps its sections in one of a few member names across NX
# versions; return the first that resolves to a non-empty list of Section objects.
def _sections_of(builder, candidate_names) -> list:
    for name in candidate_names:
        holder = getattr(builder, name, None)
        if holder is None:
            continue
        getter = getattr(holder, "GetSections", None)
        sections = list(getter()) if getter is not None else list(holder)
        if sections:
            return sections
    return []


def _fillet(part, feature) -> NxFillet:
    builder = part.Features.CreateEdgeBlendBuilder(feature)
    try:
        # Edge blends store edges in chainsets, each with its own radius Expression. The
        # IR fillet carries one radius, so take the first chainset's; gather all edges.
        fillet = NxFillet(name=feature.Name)
        chainsets = builder.GetNumberOfValidChainsets()
        for i in range(chainsets):
            edges, radius = builder.GetChainset(i)
            if i == 0:
                fillet.radius_mm = radius.Value
            for edge in _edges_of(edges):
                fillet.edges.append(edge_geometry.describe(edge))
        return fillet
    finally:
        builder.Destroy()


def _chamfer(part, feature) -> NxChamfer:
    builder = part.Features.CreateChamferBuilder(feature)
    try:
        chamfer = NxChamfer(name=feature.Name, distance_mm=_first_value(feature))
        for edge in _edges_of(builder.Edges):
            chamfer.edges.append(edge_geometry.describe(edge))
        return chamfer
    finally:
        builder.Destroy()


def _shell(part, feature) -> NxShell:
    builder = part.Features.CreateShellBuilder(feature)
    try:
        shell = NxShell(name=feature.Name, thickness_mm=_first_value(feature))
        for face in _faces_of(builder.PiercedFaces):
            shell.removed_faces.append(face_geometry.describe(face))
        return shell
    finally:
        builder.Destroy()


def _draft(part, feature) -> NxDraft:
    builder = part.Features.CreateDraftBuilder(feature)
    try:
        pull = builder.PullDirection
        draft = NxDraft(
            name=feature.Name,
            angle_degrees=_first_value(feature) * _RAD_TO_DEG,
            pull=[pull.X, pull.Y, pull.Z],
        )
        for face in _faces_of(builder.FaceCollector):
            draft.faces.append(face_geometry.describe(face))
        return draft
    finally:
        builder.Destroy()


def _hole(part, feature) -> NxHole:
    builder = part.Features.CreateHolePackageBuilder(feature)
    try:
        return NxHole(
            name=feature.Name,
            placement_face=face_geometry.describe(builder.PlacementFace),
            diameter_mm=builder.Diameter.Value,
            depth_mm=builder.Depth.Value,
            through_all=builder.ThroughAll,
        )
    finally:
        builder.Destroy()


def _pattern(part, feature, feature_tag_to_index) -> Optional[NxFeature]:
    builder = part.Features.CreatePatternFeatureBuilder(feature)
    try:
        sources = _resolve_sources(builder.GetSourceFeatures(), feature_tag_to_index)
        if not sources:
            return None  # sources were not extracted — cannot bind the pattern
        if builder.LayoutType == "Circular":
            return _circular_pattern(feature, builder, sources)
        return _rectangular_pattern(feature, builder, sources)
    finally:
        builder.Destroy()


def _rectangular_pattern(feature, builder, sources) -> NxRectangularPattern:
    return NxRectangularPattern(
        name=feature.Name,
        source_feature_indices=sources,
        count_x=builder.XCount,
        count_y=builder.YCount,
        step_x=_step(builder.XDirection, builder.XPitch),
        step_y=_step(builder.YDirection, builder.YPitch),
    )


def _circular_pattern(feature, builder, sources) -> NxCircularPattern:
    axis_point = builder.AxisPoint
    axis_dir = builder.AxisDirection
    return NxCircularPattern(
        name=feature.Name,
        source_feature_indices=sources,
        count=builder.CircularCount,
        angle_degrees=builder.CircularAngle * _RAD_TO_DEG,
        axis_point=[axis_point.X, axis_point.Y, axis_point.Z],
        axis_dir=[axis_dir.X, axis_dir.Y, axis_dir.Z],
    )


def _mirror(part, feature, feature_tag_to_index) -> Optional[NxFeature]:
    builder = part.Features.CreateMirrorBuilder(feature)
    try:
        sources = _resolve_sources(builder.GetSourceFeatures(), feature_tag_to_index)
        if not sources:
            return None
        origin = builder.PlaneOrigin
        normal = builder.PlaneNormal
        return NxMirror(
            name=feature.Name,
            source_feature_indices=sources,
            plane_origin=[origin.X, origin.Y, origin.Z],
            plane_normal=[normal.X, normal.Y, normal.Z],
        )
    finally:
        builder.Destroy()


# The IR indices of the patterned/mirrored source features that were extracted.
def _resolve_sources(sources, feature_tag_to_index) -> List[int]:
    resolved = []
    for source in sources:
        index = feature_tag_to_index.get(source.Tag)
        if index is not None:
            resolved.append(index)
    return resolved


def _step(direction, pitch) -> List[float]:
    return [direction.X * pitch, direction.Y * pitch, direction.Z * pitch]


# The IR sketch index of the first section curve that belongs to an extracted sketch.
def _sketch_index_of(section, curve_tag_to_sketch) -> int:
    for curve in section.GetOutputCurves():
        index = curve_tag_to_sketch.get(curve.Tag)
        if index is not None:
            return index
    return -1


# A feature's primary scalar (a blend's radius, a chamfer's distance, ...) in base units (mm).
def _first_value(feature) -> float:
    expressions = feature.GetExpressions()
    return expressions[0].Value if len(expressions) > 0 else 0


def _edges_of(collector):
    for obj in collector.GetObjects():
        if isinstance(obj, NXOpen.Edge):
            yield obj


def _faces_of(collector):
    for obj in collector.GetObjects():
        if isinstance(obj, NXOpen.Face):
            yield obj
