# SPDX-License-Identifier: GPL-2.0-only
"""Translates NX history features into Oblikovati recipe features.

Unsupported kinds are recorded in the report and skipped (never STEP-substituted).
Patterns and mirror remap their source program indices via the IR->recipe index map,
and are skipped if any source was itself skipped.
"""
from __future__ import annotations

from typing import Dict, List, Optional, Tuple

from ..model.feature import (
    NxCircularPattern,
    NxExtentDirection,
    NxExtrude,
    NxFeature,
    NxLoft,
    NxMirror,
    NxOperation,
    NxRectangularPattern,
    NxReplicatingFeature,
    NxRevolve,
    NxSweep,
)
from ..model.dressup import NxChamfer, NxDraft, NxFillet, NxHole, NxShell
from ..recipe.feature import (
    CircPatternData,
    ExtrudeData,
    FeatureData,
    LoftData,
    LoftSectionData,
    MirrorData,
    RectPatternData,
    RevolveData,
    SweepData,
)
from . import dressup_translator
from .report import ExportReport
from .units import DEG_TO_RAD, MM_TO_CM, scale_point


class FeatureTranslator:
    def __init__(self, report: ExportReport) -> None:
        self._report = report

    def translate(
        self, feature: NxFeature, source_index: Dict[int, int]
    ) -> Optional[FeatureData]:
        """Returns the recipe feature, or None if unsupported or a source was skipped."""
        if isinstance(feature, NxExtrude):
            return _translate_extrude(feature)
        if isinstance(feature, NxRevolve):
            return _translate_revolve(feature)
        if isinstance(feature, NxSweep):
            return _translate_sweep(feature)
        if isinstance(feature, NxLoft):
            return _translate_loft(feature)
        if isinstance(feature, NxRectangularPattern):
            return self._translate_rect_pattern(feature, source_index)
        if isinstance(feature, NxCircularPattern):
            return self._translate_circ_pattern(feature, source_index)
        if isinstance(feature, NxMirror):
            return self._translate_mirror(feature, source_index)
        if isinstance(feature, NxFillet):
            return dressup_translator.fillet(feature)
        if isinstance(feature, NxChamfer):
            return dressup_translator.chamfer(feature)
        if isinstance(feature, NxShell):
            return dressup_translator.shell(feature)
        if isinstance(feature, NxDraft):
            return dressup_translator.draft(feature)
        if isinstance(feature, NxHole):
            return dressup_translator.hole(feature)
        self._report.unsupported("feature", type(feature).__name__)
        return None

    def _translate_rect_pattern(
        self, pattern: NxRectangularPattern, source_index: Dict[int, int]
    ) -> Optional[FeatureData]:
        ok, sources = self._resolve_sources(pattern, source_index)
        if not ok:
            return None
        payload = RectPatternData(
            source=sources,
            count_x=pattern.count_x,
            count_y=pattern.count_y,
            step_x=scale_point(pattern.step_x),
            step_y=scale_point(pattern.step_y),
        )
        return FeatureData(
            kind="rectangular-pattern",
            name=_name_of(pattern),
            payload_alias="rectangularPattern",
            payload=payload,
        )

    def _translate_circ_pattern(
        self, pattern: NxCircularPattern, source_index: Dict[int, int]
    ) -> Optional[FeatureData]:
        ok, sources = self._resolve_sources(pattern, source_index)
        if not ok:
            return None
        spread = 360.0 if pattern.angle_degrees == 0 else pattern.angle_degrees
        payload = CircPatternData(
            source=sources,
            count=pattern.count,
            angle=spread * DEG_TO_RAD,
            axis_point=scale_point(pattern.axis_point),
            axis_dir=list(pattern.axis_dir),
        )
        return FeatureData(
            kind="circular-pattern",
            name=_name_of(pattern),
            payload_alias="circularPattern",
            payload=payload,
        )

    def _translate_mirror(
        self, mirror: NxMirror, source_index: Dict[int, int]
    ) -> Optional[FeatureData]:
        ok, sources = self._resolve_sources(mirror, source_index)
        if not ok:
            return None
        payload = MirrorData(
            source=sources,
            origin=scale_point(mirror.plane_origin),
            normal=list(mirror.plane_normal),
        )
        return FeatureData(
            kind="mirror", name=_name_of(mirror), payload_alias="mirror", payload=payload
        )

    # Maps a replicating feature's IR source indices to recipe program indices. Fails
    # (reports + returns False) if any source was skipped, since the pattern can't bind.
    def _resolve_sources(
        self, feature: NxReplicatingFeature, source_index: Dict[int, int]
    ) -> Tuple[bool, List[int]]:
        resolved: List[int] = []
        for ir in feature.source_feature_indices:
            if ir not in source_index:
                self._report.warn(
                    f"{type(feature).__name__} '{feature.name}' references feature {ir}, "
                    "which was not translated; skipped"
                )
                return False, []
            resolved.append(source_index[ir])
        return True, resolved


def _translate_extrude(extrude: NxExtrude) -> FeatureData:
    payload = ExtrudeData(
        sketch=extrude.sketch_index,
        profiles=[extrude.profile_index],
        operation=_operation_name(extrude.operation),
        extent="distance",
        direction=_direction_name(extrude.direction),
        distance=extrude.distance * MM_TO_CM,
        distance2=extrude.second_distance * MM_TO_CM if extrude.second_distance != 0 else None,
        taper=extrude.taper_degrees * DEG_TO_RAD if extrude.taper_degrees != 0 else None,
    )
    return FeatureData(
        kind="extrude", name=_name_of(extrude), payload_alias="extrude", payload=payload
    )


def _translate_revolve(revolve: NxRevolve) -> FeatureData:
    # Own-centerline mode: the profile sketch carries the axis as a centerline, so no
    # axis fields are emitted. Angle 0 (full revolution) is left unset.
    payload = RevolveData(
        sketch=revolve.sketch_index,
        profile=revolve.profile_index,
        operation=_operation_name(revolve.operation),
        angle=revolve.angle_degrees * DEG_TO_RAD if revolve.angle_degrees != 0 else None,
    )
    return FeatureData(
        kind="revolve", name=_name_of(revolve), payload_alias="revolve", payload=payload
    )


def _translate_sweep(sweep: NxSweep) -> FeatureData:
    payload = SweepData(
        sketch=sweep.profile_sketch_index,
        profile=sweep.profile_index,
        path=[scale_point(p) for p in sweep.path],
        closed=True if sweep.closed else None,
        operation=_operation_name(sweep.operation),
    )
    return FeatureData(kind="sweep", name=_name_of(sweep), payload_alias="sweep", payload=payload)


def _translate_loft(loft: NxLoft) -> FeatureData:
    payload = LoftData(
        sections=[LoftSectionData(sketch=s.sketch_index, profile=s.profile_index) for s in loft.sections],
        closed=True if loft.closed else None,
        operation=_operation_name(loft.operation),
    )
    return FeatureData(kind="loft", name=_name_of(loft), payload_alias="loft", payload=payload)


def _operation_name(operation: NxOperation) -> str:
    return operation.value


def _direction_name(direction: NxExtentDirection) -> str:
    return direction.value


def _name_of(feature: NxFeature) -> Optional[str]:
    return feature.name or None
