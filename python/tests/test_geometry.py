# SPDX-License-Identifier: GPL-2.0-only
"""Tests for the pure geometry helpers used by the (NXOpen-free) extraction math."""
import math

from oblikovati_exporter_nx.model import geometry_math
from oblikovati_exporter_nx.model import sketch_plane_math as spm


def test_midpoint():
    assert geometry_math.midpoint([0, 0, 0], [2, 4, 6]) == [1, 2, 3]


def test_average_of_empty_is_origin():
    assert geometry_math.average([]) == [0.0, 0.0, 0.0]


def test_average():
    assert geometry_math.average([[0, 0, 0], [2, 2, 2]]) == [1.0, 1.0, 1.0]


def test_unit_direction_is_normalized():
    d = geometry_math.unit_direction([0, 0, 0], [0, 0, 5])
    assert d == [0.0, 0.0, 1.0]


def test_unit_direction_of_coincident_points_is_zero():
    assert geometry_math.unit_direction([1, 1, 1], [1, 1, 1]) == [0.0, 0.0, 0.0]


def test_fit_plane_to_xy_square():
    pts = [[0, 0, 0], [40, 0, 0], [40, 30, 0], [0, 30, 0]]
    frame = spm.fit(pts)
    # The square lies in z=0, so the fitted normal is +/-Z and points project cleanly.
    normal = spm.cross(frame.xaxis, frame.yaxis)
    assert abs(abs(normal[2]) - 1.0) < 1e-9
    # A corner projects to a finite 2D coordinate within the square's extent.
    u, v = frame.to_2d([40, 30, 0])
    assert math.isfinite(u) and math.isfinite(v)


def test_fit_degenerate_points_falls_back_to_world_xy():
    frame = spm.fit([[1, 1, 1]])
    assert frame.xaxis == [1.0, 0.0, 0.0]
    assert frame.yaxis == [0.0, 1.0, 0.0]


def test_normalize_zero_vector():
    assert spm.normalize([0, 0, 0]) == [0.0, 0.0, 0.0]
