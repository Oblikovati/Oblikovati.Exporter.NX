# SPDX-License-Identifier: GPL-2.0-only
"""Builds a geometric face descriptor (centroid) from an NX face.

For dress-up face selections (shell/draft/hole placement, ADR-0040). The centroid is the
average of the face's edge vertices — the face centre for a planar face and a stable
representative otherwise. The normal is left unset (Oblikovati's resolver matches a
centroid-only descriptor by nearness, unambiguous for distinct faces); a normal would
need the UF face-props API, a refinement for symmetric geometry.
"""
from __future__ import annotations

from ..model import geometry_math
from ..model.dressup import NxFaceDescriptor


def describe(face) -> NxFaceDescriptor:
    points = []
    for edge in face.GetEdges():
        a, b = edge.GetVertices()
        points.append([a.X, a.Y, a.Z])
        points.append([b.X, b.Y, b.Z])
    return NxFaceDescriptor(centroid=geometry_math.average(points), normal=[0.0, 0.0, 0.0])
