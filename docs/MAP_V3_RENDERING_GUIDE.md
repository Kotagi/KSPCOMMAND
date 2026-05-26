# Map V3 — Rendering guide (living document)

How each **object type** is drawn on the modular map. Update a section when that element’s layer ships.

**Status:** Phase 3 complete — `starMarker` + `planetOrbit` + `planetBody` (textures + oriented mesh LOD); other elements not rendered.

---

## Global pipeline (all elements)

1. **Telemetry** → `buildMapContext(telemetry)` → bodies at UT in root frame  
2. **Planner** (per kind) → `TrajectorySegment[]` with `role` / `points` in root meters  
3. **SceneFrame** → `toScenePoint` / polyline mapping → scene units  
4. **Layer** → Three.js / R3F primitives (material, LOD, pick id)  
5. **Flags** → layer omitted entirely when `MapV3LayerFlags[kind] === false`

Shared rules (from V2):

- Positions: `bodies[].positionRootRelativeMeters` at `gameUniversalTimeSeconds`
- Colors: `getKspBodyMapColor` + generated fallback
- No hardcoded stock body name lists for geometry

---

## Element catalog

| Kind | Layer | Phase | Status | Doc section |
|------|-------|-------|--------|-------------|
| `starMarker` | `StarMarkerLayer` | 1 | **Implemented** | § Star marker |
| `planetOrbit` | `PlanetOrbitLayer` | 2 | **Implemented** | § Planet orbit |
| `planetBody` | `PlanetBodyLayer` | 3.1–3.4 | **Implemented** | Body + texture + orientation specs |
| `moonOrbit` | `MoonOrbitLayer` | 4 | Shipped | § Moon orbit |
| `moonBody` | `MoonBodyLayer` | 5 | Not started | § Moon body |
| `vesselMarker` | `VesselMarkerLayer` | 6 | Not started | § Vessel marker |
| `bodyLod` | (body layers) | 7 | Not started | § Body LOD |
| `vesselOrbit` | `VesselOrbitLayer` | 8 | Not started | § Vessel orbit |
| `bodyLabel` | `BodyLabelLayer` | 9 | Not started | § Body label |
| `futureRoute` | `FutureRouteLayer` | 10 | Not started | § Future route |
| `soiRing` | `SoiRingLayer` | 11 | Not started | § SOI ring |
| `selection` | `SelectionLayer` | 12 | Not started | § Selection |

---

## § Star marker

**KSP reference:** Star centered in solar system view (stock: Sun at origin).

| Field | Value |
|-------|-------|
| Data source | `resolveSystemAnchors(ctx)` → `starBody(ctx)` / `telemetry.rootBody` |
| Segment builder | `buildStarMarkerSegments` → one point at `positionRootRelativeMeters` |
| Primitive | `sphereGeometry` + `meshStandardMaterial` |
| Texture | `getSunTexture()` procedural map |
| Color | `getKspBodyMapColor(bodyName)` |
| Emissive | `#ffaa00`, intensity `1.2` |
| Frame | `toScenePoint(seg.points[0], starMarkerDrawFrame(sceneFrame))` — world shift when body/SOI focused |
| LOD | `bodyMeshRadius` with `SUN_MIN_MESH_RADIUS` floor when `bodyName === hierarchy.rootBody` |
| Render order | `10` |
| Pick id | Deferred (Phase 12 selection) |

**Multi-star (future):** `resolveSystemAnchors` returns N anchors; layer already renders `segments.map` → one mesh per anchor. Do not hardcode `"Sun"`; use `ctx.rootBody` and segment `bodyName`. Alternate systems will use per-snapshot `rootBody` or a store-selected active root.

**Files:** `map-v3/elements/starMarker/*`, `planner/buildSegments.ts`, `scene/v3/layers/StarMarkerLayer.tsx`

---

## § Planet orbit

**Spec:** [`MAP_V3_PLANET_ORBIT_SPEC.md`](MAP_V3_PLANET_ORBIT_SPEC.md)

| Field | Value |
|-------|-------|
| Geometry | `resolvePlanetOrbitSourcePoints` — **telemetry samples first** (≥2), else analytic; see planet spec |
| DLL samples | **128** per period (`BodyOrbitPathSampleCount`) |
| Web densify | Arc-length to **512** verts (`PLANET_ORBIT_STYLE.trailVertices`) |
| Segment builder | `buildPlanetOrbitSegments` → `kind: planetOrbit` |
| Primitive | `GradientDirectionalOrbitTrail` → one closed `Line` (512 verts + close duplicate) |
| Color | `useKspBodyMapColor(bodyName)` — stock table + dev defaults; see [`PLANET_ORBIT_COLOR_GUIDE.md`](PLANET_ORBIT_COLOR_GUIDE.md) |
| Opacity | Motion tail: `ORBIT_TRAIL_TAIL_ATTACH` 1.0 at body, `ORBIT_TRAIL_TAIL_LEAD` 0.25 one step prograde, linear ramp to 1.0 via `closedRingHalfGradientOpacities` + `vertexColors` |
| Line width | `1.0` on single ring (`progradeLineWidthFactor` only on split-trail fallback) |
| Prograde direction | `sampleUniversalTimes` when telemetry samples used (omitted for analytic rings); see orbit guide §2 |
| Tone mapping | `Map3DV3` `NoToneMapping` — see [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) |
| Frame | `useV3SceneTrails` → `toScenePoints` with normal `sceneFrame` |
| Visibility | `visibleBodyNames` ∩ `hierarchy.planetNames` |
| Flags | `MAP_V3_LAYERS_PHASE2.planetOrbit` |

**Files:** `map-v3/elements/planetOrbit/*`, `map-v3/useMapV3Trails.ts`, `scene/v3/layers/PlanetOrbitLayer.tsx`, `OrbitTrailV3.tsx`, `scene/GradientDirectionalOrbitTrail.tsx`

---

## § Planet body

**KSP reference:** Colored planet on heliocentric trail; to-scale mesh when zoomed in, fixed map icon when zoomed out.

| Field | Value |
|-------|-------|
| Data source | `hierarchy.planetNames` → `bodyByName` → `positionRootRelativeMeters` |
| Segment builder | `buildPlanetBodySegments` → one point per planet, `kind: planetBody` |
| LOD | V3 `planetBodyLod.ts` — always on; mesh when `meshR/camDist > 0.12`, else icon radius `0.06` |
| Scale | `bodyMeshRadius` + `hostPlanetOpen` (solar floor `0.05` when host closed) |
| Primitive | Mesh: oriented group → pole frame → `TexturedPlanetBody` or `FlatPlanetBody`. Icon: `PlanetBodyDot` |
| Texture | Plugin export → `GET /assets/bodies/{Name}.jpg?rev=`; loader `flipY = true` |
| Orientation | `bodyOrientationRootRelative` @ UT; icon LOD not rotated |
| Color fallback | `useKspBodyMapColor` when texture pending/failed/unsupported |
| Frame | `toScenePoint(seg.points[0], sceneFrame)` |
| Visibility | `visibleBodyNames` ∩ `planetNames` (same as `PlanetOrbitLayer`) |
| Flags | `MAP_V3_LAYERS_PHASE3.planetBody` |
| Render order | mesh `1`, icon `2` |
| UI build | `112-planet-texture-flipy` |

**Specs:** [`MAP_V3_PLANET_BODY_SPEC.md`](MAP_V3_PLANET_BODY_SPEC.md), [`MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](MAP_V3_PLANET_BODY_TEXTURE_SPEC.md), [`MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md`](MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md)

**Guide:** [`MAP_V3_PHASE3_GUIDE.md`](MAP_V3_PHASE3_GUIDE.md)

**Files:** `map-v3/elements/planetBody/*`, `scene/v3/layers/PlanetBody*.tsx`, `coords/kspBodyOrientation.ts`, `assets/planetBodyTextures.ts`

---

## § Moon orbit

**Spec:** [`MAP_V3_MOON_ORBIT_SPEC.md`](MAP_V3_MOON_ORBIT_SPEC.md) — § **Standard procedure** (required reading before edits).

**Frame:** [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md) § Moon trail rings. UI `122-moon-orbit-samples-first`; DLL `resolverVersion` `"5"`.

| Piece | Location |
|-------|----------|
| Filter | `map-v3/elements/moonOrbit/filterMoonOrbit.ts` |
| Source | `resolveMoonOrbitSource.ts` (samples-first) |
| Placement | `moonOrbitPlacement.ts` → `parent(now) + offset` |
| Scene | `moonOrbitScene.ts` → `toScenePoint` only |
| Builder | `buildMoonOrbitSegments.ts` |
| Mesh LOD gate | `map-v3/PlanetBodyMeshLodContext.tsx` |
| Layer | `scene/v3/layers/MoonOrbitLayer.tsx` |
| Drawer | `OrbitTrailV3` → `GradientDirectionalOrbitTrail` (same as planet orbits) |

**Visibility:** Moon trails draw only when **parent planet** is in mesh mode (`usePlanetsInMeshMode`). Not gated by `MoonVisibilityContext` / SOI zoom alone.

**Do not** use `bodyOrientationRootRelative` or analytic Kepler rings when DLL samples exist — see spec § Forbidden patterns.

**Production flags:** `MAP_V3_LAYERS_PHASE4` in `layerFlags.ts`.

---

## § Moon body

*Not implemented.*

---

## § Vessel marker

*Not implemented.*

---

## § Body LOD

*Not implemented.*

---

## § Vessel orbit

*Not implemented.*

---

## § Body label

*Not implemented.*

---

## § Future route

*Not implemented.*

---

## § SOI ring

*Not implemented.*

---

## § Selection

*Not implemented.*

---

## Lessons learned (append as we go)

| Date | Topic | Note |
|------|-------|------|
| 2026-05-21 | Phase 0 | V3 forked from V2 patterns; blank scene uses same CEF-safe star background as V2 (no postprocessing). |
| 2026-05-21 | Phase 1 | Star uses v2 `starBody` + modular `SystemAnchor`; emissive mesh matches V2 `StarLayer` (CEF-safe, no EffectComposer). |
| 2026-05-21 | Phase 1 camera | V3 star-only view must frame the star, not full-system bounds; star uses `focus: null` (not moon LOD `displayFocus`). |
| 2026-05-21 | Phase 2 | Planet orbits: v3-native `buildPlanetOrbitSegments` + `OrbitTrailV3`; decouple [`MAP_V3_DECOUPLE_PLAN.md`](MAP_V3_DECOUPLE_PLAN.md). |
| 2026-05-21 | Phase 2 | Motion-tail opacity on single closed ring (`81-orbit-motion-tail`); see [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) §2. |
| 2026-05-21 | Phase 2 | Samples-first planet geometry (`84-orbit-samples-first`); aligns icons with KSP trails. |
| 2026-05-21 | Phase 2 | **128** DLL orbit samples + **512** web densify; UI `92-v3-planet-orbit-native`; tuning [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) §12. |
| 2026-05-22 | Phase 2 | Heliocentric frame fix — unified `getRelativePositionAtUT` for Sun-children; UI `94-heliocentric-relative-unified`; [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md). |
| 2026-05-22 | Phase 3.1 | Planet bodies: v3 `buildPlanetBodySegments` + mesh/icon LOD; UI `95-v3-planet-bodies`. |
| 2026-05-22 | Phase 3.3 | Plugin JPEG export + `TexturedPlanetBody`; guide [`MAP_V3_PHASE3_GUIDE.md`](MAP_V3_PHASE3_GUIDE.md). |
| 2026-05-23 | Phase 3.4 | Oriented mesh + pole frame; schema v10; dev spin axis. |
| 2026-05-23 | Phase 3 closeout | `flipY = true`; UI `112-planet-texture-flipy`; texture meridian debt documented. |
