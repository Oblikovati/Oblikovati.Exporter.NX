<!-- SPDX-License-Identifier: GPL-2.0-only -->
# NXOpen binding audit

The extraction adapter compiles against a hand-written **facade stub** (`stubs/NXOpen.Stubs`)
and binds the real `NXOpen.dll` at load time inside NX (reference-assembly model). With no
NX install available, the binding cannot be compile-verified against the genuine assemblies;
this audit instead checks each member the adapter uses against the **official NXOpen .NET
API reference** (docs.sw.siemens.com / the NX 2022 .NET reference). It is the next-best
verification short of a real-assembly build.

## ✅ Verified correct (facade matches the .NET reference)

| Member | Signature | Used by |
|---|---|---|
| `Expression.RightHandSide` | `string` (get/set) | ExpressionExtractor |
| `Expression.Type` | `string` (get) | ExpressionExtractor |
| `Expression.Value` | `double` (get/set) — base units | feature scalars |
| `Expression.Units` | `NXOpen.Unit` (get/set) | ExpressionExtractor |
| `Expression.GetOwningFeature()` | `NXOpen.Features.Feature` | ExpressionExtractor |
| `ExpressionCollection.ToArray()` | `Expression[]` (also `IEnumerable`) | ExpressionExtractor |
| `Face.GetEdges()` | `NXOpen.Edge[]` | NxFaceGeometry |
| `Edge.GetVertices(out Point3d, out Point3d)` | `void` | NxEdgeGeometry |
| `Sketch.GetAllGeometry()` | `NXObject[]` | SketchExtractor |
| `Line.StartPoint` / `Line.EndPoint` | `Point3d` (get) | SketchExtractor |
| `ExtrudeBuilder.Section` | `Section` (get/set) | extrude |
| `ExtrudeBuilder.Limits` | `Limits` (get) — *property exists* | extrude |

So the **core read surface** (parameters, edge/face geometry, sketch curves, collections) is
correct.

## ❌ Confirmed divergent — needs rework against the real API

| Area | Facade (wrong) | Real API (from the reference) |
|---|---|---|
| Edge blend edges/radius | `EdgeBlendBuilder.Edges` (ScCollector) + radius from `feature.GetExpressions()` | **chainsets**: `GetNumberOfValidChainsets()` + `GetChainset(int, out ScCollector, out Expression radius)` — **CORRECTED in code** |
| Extrude/revolve limits | `Features.Limits` with `StartExtend/EndExtend.Value` | `Limits` is `NXOpen.GeometricUtilities.Limits`; sub-structure not yet confirmed |

## ⚠️ Not yet verified (fabricated/flattened — almost certainly diverge)

The remaining feature builders were modelled best-effort and have **not** been checked against
the reference; expect each to differ like the edge-blend did:
`ChamferBuilder`, `ShellBuilder`, `DraftBuilder`, `HolePackageBuilder`,
`PatternFeatureBuilder`, `MirrorBuilder`, and `Section.GetOutputCurves` / `Limits` internals,
plus `Component.*` / `ComponentAssembly.RootComponent` / `Part.*` / `Session.*`.

## Improvement found

`Sketch` exposes `Origin` (`Point3d`) and `Orientation` (`NXMatrix`) — the real sketch plane.
The extractor currently *fits* a plane from the curve points (works, and is unit-tested); a
follow-up can use the genuine frame instead.

## To finish verification

Build the adapter against the real assemblies — `dotnet build src/Exporter.NX.Entry
-p:UseNxStubs=false -p:NxOpenDir=<…/NXBIN/managed or UGOPEN/NET>` — on a machine with NX (or
the NXOpen reference assemblies). The compiler then flags every remaining mismatch; fix the
builders to their real (chainset-style) APIs and re-run the fake-scene dry run. (Build only
`Exporter.NX.Entry`, not the whole solution: the test project links the stub `NXOpen`, which
would clash with the real `NXOpen.dll` on the assembly name.)

### Wired into CI

The `build` workflow has a **`binding-check`** job that runs exactly this command. It is
gated on the `NX_OPEN_DIR` repository variable (`if: vars.NX_OPEN_DIR != ''`) and targets a
self-hosted runner labelled `nx`, so it stays **skipped** on stock PRs until a runner with the
NXOpen assemblies is registered and the variable is set. Once enabled it compiles the adapter
against the genuine `NXOpen` / `NXOpen.Utilities` / `NXOpen.UF` assemblies on every push,
turning the remaining ⚠️ rows above into hard compile errors the moment a binding diverges.
