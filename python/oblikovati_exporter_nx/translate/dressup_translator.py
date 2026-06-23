# SPDX-License-Identifier: GPL-2.0-only
"""Translates NX dress-up features into recipe features with GEOMETRIC selections.

Fillet/chamfer/shell/draft/hole carry edge/face selections as geometric descriptors
(ADR-0040) — the path that lets the exporter place them without Oblikovati lineage keys.
Lengths convert mm -> cm; the draft angle converts degrees -> radians.
"""
from __future__ import annotations

from typing import Iterable, Optional

from ..model.dressup import (
    NxChamfer,
    NxDraft,
    NxEdgeDescriptor,
    NxFaceDescriptor,
    NxFeature,
    NxFillet,
    NxHole,
    NxShell,
)
from ..recipe.feature import (
    EdgeDressData,
    FaceDressData,
    FeatureData,
    GeomEdgeRefData,
    GeomFaceRefData,
    HoleData,
)
from .units import DEG_TO_RAD, MM_TO_CM, scale_point


def fillet(feature: NxFillet) -> FeatureData:
    payload = EdgeDressData(value=feature.radius_mm * MM_TO_CM)
    _add_edges(payload, feature.edges)
    return FeatureData(kind="fillet", name=_name_of(feature), payload_alias="fillet", payload=payload)


def chamfer(feature: NxChamfer) -> FeatureData:
    payload = EdgeDressData(value=feature.distance_mm * MM_TO_CM)
    _add_edges(payload, feature.edges)
    return FeatureData(kind="chamfer", name=_name_of(feature), payload_alias="chamfer", payload=payload)


def shell(feature: NxShell) -> FeatureData:
    payload = FaceDressData(value=feature.thickness_mm * MM_TO_CM)
    _add_faces(payload, feature.removed_faces)
    return FeatureData(kind="shell", name=_name_of(feature), payload_alias="shell", payload=payload)


def draft(feature: NxDraft) -> FeatureData:
    payload = FaceDressData(value=feature.angle_degrees * DEG_TO_RAD, pull=list(feature.pull))
    _add_faces(payload, feature.faces)
    return FeatureData(kind="draft", name=_name_of(feature), payload_alias="draft", payload=payload)


def hole(feature: NxHole) -> FeatureData:
    payload = HoleData(
        diameter=feature.diameter_mm * MM_TO_CM,
        depth=feature.depth_mm * MM_TO_CM,
        through_all=True if feature.through_all else None,
        type="drilled",
        geom_face=_face_ref(feature.placement_face),
        center=scale_point(feature.center) if feature.center is not None else None,
    )
    return FeatureData(kind="hole", name=_name_of(feature), payload_alias="hole", payload=payload)


def _add_edges(payload: EdgeDressData, edges: Iterable[NxEdgeDescriptor]) -> None:
    for edge in edges:
        payload.geom_edges.append(
            GeomEdgeRefData(midpoint=scale_point(edge.midpoint), direction=list(edge.direction))
        )


def _add_faces(payload: FaceDressData, faces: Iterable[NxFaceDescriptor]) -> None:
    for face in faces:
        payload.geom_faces.append(_face_ref(face))


def _face_ref(face: NxFaceDescriptor) -> GeomFaceRefData:
    return GeomFaceRefData(centroid=scale_point(face.centroid), normal=list(face.normal))


def _name_of(feature: NxFeature) -> Optional[str]:
    return feature.name or None
