# Oblikovati.Exporter.NX

A Siemens NX plugin that reads the open part/assembly through the NX Open API and
writes a native, fully-parametric **Oblikovati** document (`.opd` part / `.oad`
assembly). It transcribes NX's feature history — parameters with formulas, sketches
with constraints and dimensions, sketch-based features, datums, patterns, and the
assembly tree — so the result stays parametric and recomputes in Oblikovati. It does
**not** dump B-rep/STEP geometry.

## Architecture

The dependency flows downward; the only NXOpen-aware project is `Exporter.NX.Nx`.

```
NX live session
   │   Exporter.NX.Nx        NXOpen adapter  (links real NXOpen, or a stub in CI)
   ▼
Exporter.NX.Model           NX-neutral IR   (plain POCOs, zero NXOpen refs)
   │   Exporter.NX.Translate translation core (feature-mapping registry — PURE)
   ▼
Exporter.NX.Recipe          Oblikovati recipe POCOs + YAML emitter (YamlDotNet)
   ▼
.opd / .oad
```

The pivot is the **NX-neutral IR**: the adapter's only job is `NXOpen → IR`, and the
translator (`IR → recipe`) has no NXOpen dependency, so it runs and is unit-tested on
any runner. `Exporter.NX.Entry` wires the pieces and exposes the NX entry point.

## Build & test (no NX required)

```
dotnet build -c Release
dotnet test  -c Release
```

The `Exporter.NX.Nx` project links a compile-only `NXOpen` stub by default. For a real
release build on a machine with NX:

```
dotnet build -c Release -p:UseNxStubs=false -p:NxOpenDir="<UGOPEN managed assemblies>"
```

## Round-trip validation

`scripts/roundtrip.sh <oblikovati-cli>` emits golden documents with the emitter and
opens each with the real Oblikovati reader, catching schema drift. CI runs this as a
separate job (`build.yml`).

## Distribution

Releases ship a zip laid out for `UGII_USER_DIR` (`deploy/`). See `deploy/README.md`
for install steps. Pushing a `v*` tag runs `.github/workflows/release.yml`, which (on a
self-hosted runner with NX) publishes the plugin against the real NXOpen assemblies,
runs `scripts/package.sh` to stage `deploy/` + the managed DLLs (excluding the NXOpen
stub), and attaches the zip to the release. To build a zip by hand:

```
dotnet publish src/Exporter.NX.Entry -c Release -p:UseNxStubs=false -p:NxOpenDir="<NX managed dir>" -o publish
scripts/package.sh publish 0.1.0 Oblikovati.Exporter.NX-0.1.0.zip
```

In NX the **Export to Oblikovati** menu item runs the plugin, which writes the `.opd`
(or `.oad` + components) next to the source part and shows a summary in the listing
window.

## License

GPL-2.0-only.
