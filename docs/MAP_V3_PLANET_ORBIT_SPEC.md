# Map V3 — Planet orbit paths (element specification)

| Field | Value |
|-------|-------|
| **Document ID** | MAP-V3-PLANET-ORBIT-001 |
| **Revision** | 1.0 (2026-05-21) |
| **Phase** | 2 |
| **Scope** | Heliocentric planet orbit polylines only — no moons, no planet meshes, no vessel |

## References

| Source | Role |
|--------|------|
| KSP in-game map | Per-body trail colors and closed heliocentric paths |
| V1 `BodyOrbitsLayer` / `DirectionalOrbitTrail` | Retrograde/prograde split styling |
| V2 `PlanetOrbitLayer` / `OrbitTrailV2` | Planet-only filter + trail renderer |
| V2 `TrajectoryPlanner` `BodyOrbit` + `{ planetOnly: true }` | Path geometry (samples + analytic fallback) |
| `docs/MAP_V3_RENDERING_GUIDE.md` | Living § Planet orbit summary |

## Inclusion rules

| Body class | Included when |
|------------|----------------|
| **Planet (heliocentric)** | `hierarchy.planetNames` and `referenceBody === ctx.rootBody` |
| **Moon** | Never |
| **Root / Sun** | Never as orbit trail (star uses `starMarker`) |
| **Vessel** | Never |
| **Planet mesh** | Out of scope (Phase 3 `planetBody`) |

V3 delegates geometry to v2 `buildSegments(ctx, "BodyOrbit", { planetOnly: true })`, then maps `role` → `kind: "planetOrbit"` and re-filters with `isPlanetBody`.

## Data pipeline

1. **Telemetry** — `bodyOrbitPaths[]` samples in root frame at UT.
2. **Planner** — `bodyOrbitSegmentsForPath` / analytic fallback (shared with v2).
3. **Quality** — `resolveTrailRenderMode` may skip low-quality trails.
4. **Densify** — `buildPlanetOrbitSegments` arc-length resamples to **512** vertices per period (`PLANET_ORBIT_STYLE.trailVertices`; telemetry ships ~48).
5. **Segment** — `TrajectorySegment` with `points`, `anchorIndex`, `closed`, `bodyName`, `referenceBody`.

## Rendering pipeline

1. `PlanetOrbitLayer` → `useV3RootSegments(ctx, "planetOrbit")` → filter `visibleBodyNames` + `planetNames`.
2. `useV3SceneTrails` → `toScenePoints` with normal `sceneFrame` (moon LOD focus).
3. `OrbitTrailV3` → `splitOrbitTrailHalves` → two `@react-three/drei` `Line` primitives per trail.
4. **Depth / order** — Lines in scene group after star; no pick ids (Phase 12).

## Color and style customization

| Knob | File | Effect |
|------|------|--------|
| Per-body hex color | `web/src/scene/bodyMapColors.ts` `KSP_BODY_MAP_COLORS` | Orbit line hue |
| Default fallback color | `DEFAULT_BODY_COLOR` in same file | Unknown bodies |
| Retro/prograde split | `web/src/scene/splitOrbitTrailHalves.ts` | Opacity curve along trail |
| Line widths | `web/src/map-v3/elements/planetOrbit/planetOrbitStyle.ts` | `retrogradeLineWidth`, `progradeLineWidthFactor` |
| Trail vertex count | `planetOrbitStyle.ts` `trailVertices` (default **512**) | Smooth closed rings vs performance |
| Hidden paths | `resolveTrailRenderMode` in `web/src/coords/buildBodyOrbitTrail.ts` | Skip low-quality trails |
| Layer on/off | `MAP_V3_LAYERS_PHASE2` in `web/src/map-v3/layerFlags.ts` | `planetOrbit: true` |

## Acceptance IDs

| ID | Criterion |
|----|-----------|
| P2-01 | Colored closed/heliocentric trails for stock planets orbiting root |
| P2-02 | No Mun/Minmus or other moon rings |
| P2-03 | No planet spheres or vessel marker |
| P2-04 | Sun still visible (`starMarker`) |
| P2-05 | Colors match v1/v2 side-by-side on same flight |
| P2-06 | `MapHudV3` shows **v3 phase 2 — planet orbits**; no console errors on load/Recenter |

## Verification

**Automated**

- `buildPlanetOrbitSegments.test.ts` — ≥1 segment, planets only, `referenceBody` is root
- `MapComposer.test.ts` — phase 2 → `["StarMarkerLayer", "PlanetOrbitLayer"]`
- `npm test` / `npm run build`

**Manual**

1. View **3D Map V3** with live or fixture telemetry.
2. Compare **3D WebGL (v1)** on same flight for trail presence and hue.
3. Recenter — full solar bounds (not star-only camera).

## Revision history

| Rev | Date | Change |
|-----|------|--------|
| 1.0 | 2026-05-21 | Initial Phase 2 spec |
