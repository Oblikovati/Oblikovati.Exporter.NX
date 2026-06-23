# SPDX-License-Identifier: GPL-2.0-only
"""NXOpen adapter layer.

Only the modules in this package import ``NXOpen``; everything downstream consumes the
NX-neutral IR. These modules are never imported by the test suite or on CI (no NXOpen is
available there) — importing them requires a live NX Python interpreter. The package
``__init__`` deliberately imports nothing, so ``import oblikovati_exporter_nx`` stays
NXOpen-free.
"""
