# SPDX-License-Identifier: GPL-2.0-only
"""Builds a geometric edge descriptor (midpoint + direction) from an NX edge.

The selection form a dress-up (fillet/chamfer) carries so Oblikovati can rebind it
without lineage keys (ADR-0040). The midpoint/direction come from the edge's end
vertices: exact for a straight edge, and a stable representative + sign-agnostic hint
for a curved one (the resolver also uses tolerance and uniqueness).

In NXOpen Python, ``Edge.GetVertices()`` returns the two end points as a tuple.
"""
from __future__ import annotations

from ..model import geometry_math
from ..model.dressup import NxEdgeDescriptor


def describe(edge) -> NxEdgeDescriptor:
    a, b = edge.GetVertices()
    pa = [a.X, a.Y, a.Z]
    pb = [b.X, b.Y, b.Z]
    return NxEdgeDescriptor(
        midpoint=geometry_math.midpoint(pa, pb),
        direction=geometry_math.unit_direction(pa, pb),
    )
