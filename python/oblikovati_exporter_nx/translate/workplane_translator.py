# SPDX-License-Identifier: GPL-2.0-only
"""Maps an NX datum plane to a fixed-frame work plane (origin mm -> cm, two unit axes).

A fixed frame carries the datum's solved geometry faithfully without re-deriving the NX
construction.
"""
from __future__ import annotations

from ..model.workfeature import NxWorkPlane
from ..recipe.workfeature import WorkFeatureData
from .units import scale_point


def translate(plane: NxWorkPlane) -> WorkFeatureData:
    return WorkFeatureData(
        collection="plane",
        kind="fixed-frame",
        position=scale_point(plane.origin),
        xaxis=list(plane.xaxis),
        yaxis=list(plane.yaxis),
    )
