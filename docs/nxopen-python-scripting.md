# NX Open Python scripting — constraints honoured by the journal edition

This records the Siemens NX recommendations and constraints the Python journal
(`python/`) is built around, with the sources consulted. It is the rationale for why the
journal is shaped the way it is.

## Why a journal (the signing situation)

- A **compiled NX Open application** (a DLL/`.so`, e.g. the C# add-in loaded as a shared
  library) requires, for end users **without** an Author-class license, that the binary be
  **digitally signed** with a Siemens-issued certificate. Where that certificate cannot be
  obtained, the compiled add-in cannot be deployed.
- A **Python journal** is not a compiled, loaded binary — it is *played* through NX's
  journaling mechanism (interactively, via `run_journal`, or from a menu/User Tool). There
  is no DLL to sign, which is the blocker this edition removes. Note the license model
  still applies in general (unsigned NX Open code historically wants an Author-class
  license to run); confirm the seat's entitlement against the Siemens agreement. This is
  called out in `deploy-python/README.md`.

## Constraints the code honours

1. **Embedded interpreter, no third-party packages.** `NXOpen` is only importable inside
   NX's embedded CPython, and that interpreter has no `pip` packages. So:
   - the exporter package has **zero runtime dependencies** (it emits YAML with its own
     small writer rather than PyYAML), and
   - only the `nx/` layer imports `NXOpen`; everything else is NXOpen-free and runs on a
     stock Python for tests/CI. The package `__init__` imports nothing NXOpen.
2. **Python version.** The embedded interpreter's version is tied to the NX release
   (Python 3.x on NX 12+, 3.8–3.10 on recent releases). The code targets **Python 3.8**
   (uses `from __future__ import annotations`, `typing` generics, no `match`/walrus-only
   features) so it runs on the oldest supported embedded interpreter.
3. **Every journal needs a session.** Entry obtains `NXOpen.Session.GetSession()` and
   reads `session.Parts.Work` (`nx/session_adapter.py`).
4. **`out` parameters become return tuples in Python.** NX Open's .NET `out` parameters are
   returned as tuples by the Python binding, so the adapter unpacks e.g.
   `a, b = edge.GetVertices()` and `origin, orientation = component.GetPosition()`.
5. **Reading features is read-only.** Reading an existing feature requires creating its
   builder, which NX records on the undo stack. The adapter sets an **invisible undo mark**
   before extraction and rolls back to it afterwards, and always `Destroy()`s builders
   (never `Commit`s), so an export never modifies the open part.
6. **Stable identity via `.Tag`.** NX Open may hand back a fresh Python wrapper per call for
   the same underlying object, so identity maps (curve→sketch, feature→index) key on the
   stable integer `.Tag`, not Python object identity.
7. **Menu integration without a DLL.** The journal is bound to a button with a MenuScript
   `.men` whose `ACTIONS` names the journal file (`deploy-python/startup/oblikovati_export.men`),
   placed in `<UGII_USER_DIR>/startup`. The journal can also simply be played from
   **File ▸ Execute ▸ NX Open** / **Tools ▸ Journal ▸ Play** with no configuration.

## Reading the sketch model (constraints, dimensions, entities)

The exporter reads the **real** sketch, not just geometry:

- **Entities** (`nx/sketch_geometry.py`): line, full circle, partial arc, ellipse,
  elliptical arc and spline. The plane is fitted from the curves' 3D points and every
  point projects into that 2D frame, so absolute geometry is reconstructed without
  trusting the NX sketch-plane API.
- **Geometric constraints** (`nx/sketch_constraint_reader.py`): queried per type via
  `sketch.GetAllConstraintsOfType(NXOpen.Sketch.ConstraintClass.Geometric, <ConstraintType>)`,
  then each `SketchGeometricConstraint.GetGeometry()` yields `Sketch.ConstraintGeometry`
  items carrying the constrained object (`.Geometry`) and which defining point of it
  (`.PointType`). Those split into point operands and curve operands to form the IR
  constraint shape the translator expects (coincident, horizontal/vertical, parallel,
  perpendicular, collinear, equal-length, concentric, equal-radius, tangent, point-on-curve,
  midpoint, fix, symmetry).
- **Dimensions**: `GetAllConstraintsOfType(..., Dimension, NoCon)` →
  `SketchDimensionalConstraint`, mapped to distance/radius/diameter/angle with the driving
  expression (the parameter name or literal) read from its associated expression.
- **Fallback**: if NX returns no geometric constraints (older parts, or an API mismatch),
  coincidence is inferred from meeting line endpoints so profiles still close.

Anything that cannot be mapped is recorded in the export report (surfaced to the user),
never emitted wrong. The IR/translator/recipe side of all of this is NXOpen-free and fully
unit-tested + round-tripped through the real `oblikovati-cli`; the **read** side carries
the UNVERIFIED caveat below — the NX `ConstraintType`/`ConstraintPointType` member names
and the dimension expression accessor are best-effort vs the documented API and need a
live NX session to confirm.

## Sweep and loft

Sweep and loft are fully represented and round-tripped: a sweep is a profile (sketch +
region) plus a 3D-point path polyline; a loft is an ordered list of profile sections. The
pure core (recipe/translator/fixtures) is verified against the real `oblikovati-cli` — the
swept cylinder and lofted frustum build with the expected volume. Extraction
(`feature_extractor._sweep`/`_loft`) reads NX's swept and through-curves features via their
builders, resolving each section to its sketch index and tessellating the sweep guide into a
polyline with `sweep_path.polyline` (lines exact; arcs/splines via the UF curve evaluator —
the NX counterpart of Inventor's `GetStrokes`). The builder member names
(`SweptBuilder.SectionList`/`GuideList`, `ThroughCurvesBuilder.SectionsList`) and the UF Eval
calls are best-effort vs the documented API and carry the UNVERIFIED caveat below.

## Status of the `nx/` adapter

As with the C# add-in's NXOpen adapter, the `nx/` modules cannot be exercised off a live
NX session, so they are **excluded from CI coverage** and marked `UNVERIFIED` — their
NXOpen member shapes follow the documented API and the .NET reference the C# side was
checked against, but they need a real NX run to confirm. The pure core (recipe/model/
translate/entry) is fully tested and its output is round-tripped through the real
`oblikovati-cli`.

## Sources

- Siemens DISW — NX Open for Python reference & getting-started
  (docs.plm.automation.siemens.com NX API docs).
- NX Journaling — running journals (`run_journal`, Play), external script files, and
  binding a journal to a custom button via MenuScript:
  - <https://nxjournaling.com/content/standalone-nx-open-scripting-external-script-files>
  - <https://www.nxjournaling.com/content/start-journal-custom-button>
  - <https://www.nxjournaling.com/content/nx-open-author> (Author vs Execute license, signing)
- Community references on `UGII_USER_DIR` / `startup` / `custom_dirs.dat` menu loading and
  the embedded Python environment (community.sw.siemens.com).
