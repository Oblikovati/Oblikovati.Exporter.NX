# SPDX-License-Identifier: GPL-2.0-only
"""Oblikovati NX exporter (Python journal edition).

A pure-Python re-implementation of the NX→Oblikovati exporter, designed to run as
an unsigned NXOpen **journal** inside NX (no compiled, code-signed DLL required).
The layering mirrors the C# add-in so the two stay in lockstep:

    nx/        NXOpen adapter  (imports NXOpen; only exercised in a live NX session)
    model/     NX-neutral IR   (plain dataclasses, zero NXOpen references)
    translate/ translation core (IR -> recipe; pure, fully unit-tested)
    recipe/    Oblikovati recipe dataclasses + a dependency-free YAML emitter
    entry/     orchestration + the journal entry point glue

The package is intentionally free of third-party dependencies so it imports cleanly
under NX's embedded CPython interpreter, which has no pip packages available.
"""
