# Oblikovati.Exporter.NX

A Siemens NX → **Oblikovati** exporter that runs as an **NX Open Python journal**. It reads
the open part/assembly through the NX Open API and writes a native, fully-parametric
Oblikovati document (`.opd` part / `.oad` assembly). It transcribes NX's feature history —
parameters with formulas, sketches with constraints and dimensions, sketch-based features,
datums, patterns, and the assembly tree — so the result stays parametric and recomputes in
Oblikovati. It does **not** dump B-rep/STEP geometry.

Being a journal, it is *played* through NX's journaling mechanism — there is **no compiled,
code-signed plug-in** to install, which is useful on seats that won't load an unsigned
shared library.

## What it exports

- **Parameters / expressions** → `parameters` (units inline, formulas referencing other
  parameters by name).
- **Units** from the part's measure preferences.
- **Sketches**: the full curve set (line, circle, arc, ellipse, elliptical-arc, spline),
  real **geometric constraints** (coincident, horizontal/vertical, parallel, perpendicular,
  collinear, equal-length, concentric, equal-radius, tangent, point-on-curve, midpoint, fix,
  symmetry, ground, smooth/G2) and **dimensions** linked to parameter expressions.
- **Datum planes** → fixed-frame work features.
- **Sketch-based solids**: extrude, revolve, **sweep**, **loft**.
- **Patterns / mirror**: rectangular, circular, mirror.
- **Dress-ups**: fillet, chamfer, shell, draft, and holes (with their explicit drill
  centres). Edge/face selections are carried as geometric descriptors (ADR-0040), so they
  rebind on recompute without Oblikovati lineage keys.

Anything the exporter cannot translate is listed in an export report shown in NX's listing
window — never silently dropped, never STEP-substituted.

## Architecture

The code lives under `python/`. Dependencies flow downward; only the `nx` layer imports
`NXOpen`, and the package is **dependency-free** so it imports cleanly under NX's embedded
CPython (which has no `pip` packages).

```
NX live session
   │   oblikovati_exporter_nx/nx        NXOpen adapter   (imports NXOpen; runs only in NX)
   ▼
oblikovati_exporter_nx/model            NX-neutral IR    (plain dataclasses, no NXOpen)
   │   oblikovati_exporter_nx/translate translation core (IR → recipe — pure)
   ▼
oblikovati_exporter_nx/recipe           Oblikovati recipe dataclasses + a YAML emitter
   ▼
.opd / .oad
```

The pivot is the **NX-neutral IR**: the adapter's only job is `NXOpen → IR`, and the
translator (`IR → recipe`) has no NXOpen dependency, so it runs and is unit-tested on any
runner. `oblikovati_exporter_nx/entry` wires the pieces; `journal/oblikovati_export.py` is
the entry point NX runs.

## Install & use

The release ships a zip laid out for `UGII_USER_DIR` (see `deploy-python/README.md`):

```
<unzipped>/
  startup/
    oblikovati_export.men        NX menu entry that plays the journal
    oblikovati_export.py         the journal NX runs
    oblikovati_exporter_nx/      the exporter package (imported by the journal)
```

**Option A — play the journal (zero configuration):** in NX, **File ▸ Execute ▸ NX Open…**
(or **Tools ▸ Journal ▸ Play**) and pick `startup/oblikovati_export.py`.

**Option B — add a menu button:** set `UGII_USER_DIR` to the unzipped folder (or add it to a
`custom_dirs.dat` referenced by `UGII_CUSTOM_DIRECTORY_FILE`) and restart NX. An **Export to
Oblikovati** item appears under the File menu.

Then open a part (or assembly) and run it. The exporter writes the `.opd` (or `.oad` +
component files) next to the source part — or to the temp folder if the part is unsaved —
and shows a summary in the listing window. The export is read-only: it sets an undo mark and
rolls back any builder churn, so it never modifies the open part.

Requirements: a Siemens NX seat whose embedded Python can play journals (NX 12+ ships a
CPython interpreter with the `NXOpen` module). The package targets **Python 3.8** for the
oldest supported embedded interpreter, and needs no third-party packages. See
`docs/nxopen-python-scripting.md` for the NX scripting constraints it honours (including the
journal vs signed-binary licensing note).

## Develop & test (no NX required)

The pure core (recipe / model / translate / entry) is NXOpen-free and fully unit-tested.

```
cd python
python -m pip install -r requirements-dev.txt
python -m pytest --cov=oblikovati_exporter_nx --cov-report=term-missing
```

The `nx/` adapter is excluded from coverage — it imports `NXOpen` and only runs inside a
live NX session, so its NXOpen member shapes are confirmed there, not in CI.

## Round-trip validation

`scripts/roundtrip_python.sh <oblikovati-cli>` emits golden documents with the exporter and
opens each with the real Oblikovati reader, asserting they load and (for fully-constrained
parts) recompute to DOF 0 with closed profiles. This binds the emitter to the actual reader
and catches schema drift. The emitter is also snapshot-tested against the committed goldens
under `python/tests/goldens/`.

## CI & distribution

`.github/workflows/build.yml` runs on every push/PR:

- **python-core** — `pytest` of the pure core with the coverage gate (the project's >80% rule).
- **python-roundtrip** — builds `oblikovati-cli` and opens every generated golden, binding
  the emitter to the real Go reader.

`.github/workflows/release.yml` runs on every merge to `main`: it packages the
`UGII_USER_DIR` zip (`scripts/package_python.sh`) and attaches
`Oblikovati.Exporter.NX.Python-<ver>.zip` to a GitHub **prerelease** (`v0.1.<run>`). These
are alpha builds — validate on your NX version before production use.

To build the zip by hand:

```
scripts/package_python.sh 0.1.0 Oblikovati.Exporter.NX.Python-0.1.0.zip
```

## License

GPL-2.0-only.
