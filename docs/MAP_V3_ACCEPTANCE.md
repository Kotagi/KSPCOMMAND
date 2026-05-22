# Map V3 — Acceptance thresholds

Compare **in-game KSP map** vs web **View → 3D Map V3** when elements are enabled. Ctrl+F5 / cache-bust after installing a new bundle.

## Phase 0 — Blank foundation

| ID | Pass criteria |
|----|----------------|
| P0-01 | View dropdown includes **3D Map V3**; selecting it sets `dataset.solarView` to `3d-v3` |
| P0-02 | Viewport shows dark background (`#071019`) and starfield; no bodies, orbits, vessel, or labels |
| P0-03 | No console errors on load or camera orbit |
| P0-04 | Recenter / camera modes do not throw (shared `CameraRig`) |
| P0-05 | V1 (`3d`) and V2 (`3d-v2`) unchanged |
| P0-06 | `npm run build` and `npm test` succeed |
| P0-07 | `MapHudV3` shows phase label **v3 phase 0 — blank** |

## Phase 1 — Primary star

| ID | Pass criteria |
|----|----------------|
| P1-01 | Textured emissive star visible at map center after Recenter (same save/UT as in-game KSP solar view) |
| P1-02 | Side-by-side with **3D Map V2** — star appearance acceptable |
| P1-03 | No orbits, planet dots, vessel, labels, or SOI rings |
| P1-04 | No console errors on load, orbit, or Recenter |
| P1-05 | `MapHudV3` shows **v3 phase 1 — star** |
| P1-06 | `buildStarMarkerSegments` Vitest: 1 segment, `bodyName === rootBody`, position near origin |
| P1-07 | `composeMapV3Layers(PHASE1)` === `["StarMarkerLayer"]` |

**Automated (2026-05-21):** P1-06, P1-07 pass via `npm test`. P1-01–P1-05 require manual flight check.

## Phase 2 — Planet orbit paths

| ID | Pass criteria |
|----|----------------|
| P2-01 | Colored heliocentric planet orbit trails visible (stock save with planets) |
| P2-02 | No moon orbit rings (e.g. Mun, Minmus) |
| P2-03 | No planet mesh markers, vessel, labels, or SOI rings |
| P2-04 | Star still visible at center |
| P2-05 | Side-by-side **3D WebGL (v1)** — trail colors comparable on same flight (tune mod bodies via [`PLANET_ORBIT_COLOR_GUIDE.md`](PLANET_ORBIT_COLOR_GUIDE.md)) |
| P2-05a | **Motion tail:** bold (**1.0**) where orbit meets planet from behind; faint (**0.25**) prograde lead; linear ramp around ring so orbital direction is obvious without time warp (see [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) §2) |
| P2-05b | Planet icons on grey trails; `trailRenderMode: samples`, `liveToSample0` ≈ 0 m on stable flight ([`BODY_ORBIT_VNV.md`](BODY_ORBIT_VNV.md)) |
| P2-05c | Orbit rings visually smooth (not obvious 48-gon); telemetry `bodyOrbitPaths[].samples.length` ≈ **128** after DLL install |
| P2-06 | `MapHudV3` shows **v3 phase 2 — planet orbits**; console `94-heliocentric-relative-unified` (or current `KSP_WEB_MAP_UI_VERSION`) |
| P2-07 | Heliocentric inclination matches KSP map; QA `plane` ≈ 0° ([`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md)) |
| P2-07 | `buildPlanetOrbitSegments` Vitest: ≥1 segment, planets only, no moons |
| P2-08 | `composeMapV3Layers(PHASE2)` === `["StarMarkerLayer", "PlanetOrbitLayer"]` |
| P2-09 | No console errors on load, orbit, or Recenter |
| P2-10 | Recenter uses full solar bounds (not star-only framing) |

**Automated:** P2-07, P2-08 via `npm test`. P2-01–P2-06, P2-09–P2-10 require manual flight check.

## Phase 3.1 — Planet bodies

**Spec:** [`MAP_V3_PLANET_BODY_SPEC.md`](MAP_V3_PLANET_BODY_SPEC.md)

| ID | Pass criteria |
|----|----------------|
| P3-01 | Every visible heliocentric planet has mesh or icon in full-system view |
| P3-02 | Planet center on Phase 2 ring; AC-009 pass (`iconTrailSample0ResidualMeters` ≤ 1 m) — [`BODY_ORBIT_VNV.md`](BODY_ORBIT_VNV.md) |
| P3-03 | No moon body meshes (Mun, Minmus, etc.) |
| P3-04 | Sun drawn only via `starMarker` (no duplicate root sphere) |
| P3-05 | Body color matches orbit line per `bodyName` (stock + Customize Map overrides) |
| P3-06 | Zoom out: planets become fixed-size dots (~0.06 scene radius) |
| P3-07 | Zoom in: dots become to-scale meshes; physical radius visible when close |
| P3-08 | Host planet open: physical scale (no `0.05` solar floor on planets) |
| P3-09 | Phase 2 orbits, motion tail, and inclination unchanged |
| P3-10 | `MapHudV3` shows **v3 phase 3.1 — planet bodies**; console `95-v3-planet-bodies` |
| P3-11 | Recenter uses full solar bounds (not star-only) |
| P3-12 | No console errors on load, orbit, or Recenter |
| P3-13 | `buildPlanetBodySegments` Vitest: planets only, positions from `bodyByName` |
| P3-14 | `composeMapV3Layers(PHASE3)` === `["StarMarkerLayer", "PlanetOrbitLayer", "PlanetBodyLayer"]` |

**Automated:** P3-13, P3-14 via `npm test`. P3-01–P3-12 require manual flight check.

## Future phases

Per-element criteria added when the corresponding flag is enabled (mirror `docs/MAP_V2_ACCEPTANCE.md`).

## Automated (shared)

- `web/src/coords/*.test.ts` — shared math
- `MapComposer.test.ts` — phase 0–3 composition
- `buildPlanetBodySegments.test.ts`, `planetBodyLod.test.ts` — planet body element
