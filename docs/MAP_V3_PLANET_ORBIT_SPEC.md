# Map V3 — Planet orbit paths (element specification)

| Field | Value |
|-------|-------|
| **Document ID** | MAP-V3-PLANET-ORBIT-001 |
| **Revision** | 1.1 (2026-05-21) |
| **Phase** | 2 |
| **Scope** | Heliocentric planet orbit polylines only — no moons, no planet meshes, no vessel |
| **UI build** | `85-orbit-128-samples` |

## References

| Source | Role |
|--------|------|
| KSP in-game map | Per-body trail colors and closed heliocentric paths |
| V1 `BodyOrbitsLayer` / `DirectionalOrbitTrail` | Motion-tail styling (v3 uses single closed ring) |
| V2 `PlanetOrbitLayer` / `OrbitTrailV2` | Planet-only filter + trail renderer |
| V2 `TrajectoryPlanner` `BodyOrbit` + `{ planetOnly: true }` | Planner fallback polylines |
| [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) | Motion tail §2, vertex/sample tuning **§12** |
| [`MAP_V3_RENDERING_GUIDE.md`](MAP_V3_RENDERING_GUIDE.md) | Living § Planet orbit summary |
| [`BODY_ORBIT_VNV.md`](BODY_ORBIT_VNV.md) | Icon/trail alignment verification |

## Inclusion rules

| Body class | Included when |
|------------|----------------|
| **Planet (heliocentric)** | `hierarchy.planetNames` and `referenceBody === ctx.rootBody` |
| **Moon** | Never |
| **Root / Sun** | Never as orbit trail (star uses `starMarker`) |
| **Vessel** | Never |
| **Planet mesh** | Out of scope (Phase 3 `planetBody`) |

V3 calls v2 `buildSegments(ctx, "BodyOrbit", { planetOnly: true })` for planner segments, then **replaces** point geometry via `resolvePlanetOrbitSourcePoints` (samples-first, see below), maps `role` → `kind: "planetOrbit"`, and re-filters with `isPlanetBody`.

## Geometry source (samples first)

On a typical flight save, `bodyOrbitPaths[].validation.trailRenderMode` is **`samples`**. V3 must draw those samples, not a separate analytic Kepler ring, or planet icons will sit far off the grey trail (`liveToAnalyticMeters` can be ~100+ Mm while `liveToSample0` ≈ 0).

| Priority | Condition | Source |
|----------|-----------|--------|
| 1 | `trailRenderMode === "hidden"` | No trail (planner fallback unused for draw) |
| 2 | ≥ 2 positions in `path.samples` | Telemetry sample polyline |
| 3 | Valid `orbitElements`, sparse samples | `buildBodyOrbitTrailSegments` (analytic) |
| 4 | Else | v2 planner segment points |

Implementation: `resolvePlanetOrbitPointsFromPath` / `planetOrbitTrailUsesAnalyticSource` in `web/src/map-v3/elements/planetOrbit/densifyPlanetOrbitTrail.ts` (matches v2 `bodyOrbitSegmentsForPath`).

## Data pipeline

1. **Telemetry (DLL)** — `bodyOrbitPaths[]`: **128** root-frame samples per period (`BodyOrbitPathSampleCount` in `TelemetrySnapshotService.cs`). Fractions `i/N` for `i = 0 … N-1` (never `fraction = 1.0` at period wrap).
2. **Planner** — v2 `BodyOrbit` segments for keys, colors, visibility hints.
3. **Source resolve** — `resolvePlanetOrbitSourcePoints` → samples or analytic per table above; `resolveTrailRenderMode` may hide bad paths.
4. **Densify** — `densifyPlanetOrbitRootPoints` arc-length resamples to **512** vertices (`PLANET_ORBIT_STYLE.trailVertices`).
5. **UT** — `sampleUniversalTimes` from telemetry when source is **not** analytic; densified to 512 for prograde direction on the motion tail.
6. **Segment** — `TrajectorySegment` with `points`, `anchorIndex`, optional `sampleUniversalTimes`, `closed`, `bodyName`, `referenceBody`.

**Tuning sample vs draw counts:** [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) §12.

## Rendering pipeline

1. `PlanetOrbitLayer` → `useV3RootSegments(ctx, "planetOrbit")` → filter `visibleBodyNames` + `planetNames`.
2. `useV3SceneTrails` → `toScenePoints` with normal `sceneFrame` (moon LOD focus).
3. `OrbitTrailV3` → `GradientDirectionalOrbitTrail` (one closed `Line`; split fallback for degenerate paths).
4. **Depth / order** — Lines in scene group after star; no pick ids (Phase 12).

## Color and style customization

| Knob | File | Effect |
|------|------|--------|
| Per-body hex color | `web/src/scene/bodyMapColors.ts` `KSP_BODY_MAP_COLORS` | Orbit line hue |
| Default fallback color | `DEFAULT_BODY_COLOR` in same file | Unknown bodies |
| Motion-tail opacity | `GradientDirectionalOrbitTrail.tsx`, `orbitTrailDirectionStyle.ts` | §2 orbit guide — 1.0 trailing attach, 0.25 prograde lead, linear ramp |
| Line widths | `planetOrbitStyle.ts` | `retrogradeLineWidth`, `progradeLineWidthFactor` (split fallback) |
| **DLL samples per period** | `TelemetrySnapshotService.cs` `BodyOrbitPathSampleCount` | **128** (capture fidelity; rebuild DLL) |
| **Web draw vertices** | `planetOrbitStyle.ts` `trailVertices` | **512** (GPU smoothness; web rebuild) |
| Hidden paths | `resolveTrailRenderMode` in `coords/buildBodyOrbitTrail.ts` | Skip low-quality trails |
| Layer on/off | `MAP_V3_LAYERS_PHASE2` in `layerFlags.ts` | `planetOrbit: true` |

## Acceptance IDs

| ID | Criterion |
|----|-----------|
| P2-01 | Colored closed/heliocentric trails for stock planets orbiting root |
| P2-02 | No Mun/Minmus or other moon rings |
| P2-03 | No planet spheres or vessel marker |
| P2-04 | Sun still visible (`starMarker`) |
| P2-05 | Colors match v1/v2 side-by-side on same flight |
| P2-05a | Motion tail per orbit guide §2 |
| P2-05b | Icons on grey trails; `trailRenderMode: samples`, `liveToSample0` ≈ 0 ([`BODY_ORBIT_VNV.md`](BODY_ORBIT_VNV.md)) |
| P2-06 | `MapHudV3` shows **v3 phase 2 — planet orbits**; console logs `85-orbit-128-samples` |
| P2-10 | Recenter uses full solar bounds |

## Verification

**Automated**

- `densifyPlanetOrbitTrail.test.ts` — samples preferred over analytic when both exist
- `buildPlanetOrbitSegments.test.ts` — ≥1 segment, planets only, `referenceBody` is root
- `MapComposer.test.ts` — phase 2 → `["StarMarkerLayer", "PlanetOrbitLayer"]`
- `npm test` / `npm run build`

**Manual**

1. View **3D Map V3** with live flight telemetry.
2. Compare **3D WebGL (v1)** on same flight for trail presence and hue.
3. `scripts/verify-telemetry.ps1` — PASS on stable Kerbin orbit.
4. Recenter — full solar bounds (not star-only camera).

## Revision history

| Rev | Date | Change |
|-----|------|--------|
| 1.0 | 2026-05-21 | Initial Phase 2 spec |
| 1.1 | 2026-05-21 | Samples-first geometry; 128 DLL + 512 web densify; motion tail; §12 tuning guide cross-link; P2-05b alignment |
