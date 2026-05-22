# Map V3 — Rendering guide (living document)

How each **object type** is drawn on the modular map. Update a section when that element’s layer ships.

**Status:** Phase 2 — `starMarker` + `planetOrbit` implemented; other elements not rendered.

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
| `planetBody` | `PlanetBodyLayer` | 3 | Not started | § Planet body |
| `moonOrbit` | `MoonOrbitLayer` | 4 | Not started | § Moon orbit |
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
| Frame | `toScenePoint(seg.points[0], sceneFrame)` |
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
| Data source | `telemetry.bodyOrbitPaths` → v2 `BodyOrbit` planner → densify to **512** verts |
| Segment builder | `buildPlanetOrbitSegments` → `kind: planetOrbit` |
| Primitive | `@react-three/drei` `Line` ×2 (retrograde + prograde halves) |
| Color | `resolvePlanetOrbitColor` → `getKspBodyMapColor(bodyName)` |
| Line width | Retro: `1.0`; prograde: `0.7×` width, reduced opacity via `splitOrbitTrailHalves` |
| Frame | `useV3SceneTrails` → `toScenePoints` with normal `sceneFrame` |
| Visibility | `visibleBodyNames` ∩ `hierarchy.planetNames` |
| Flags | `MAP_V3_LAYERS_PHASE2.planetOrbit` |

**Files:** `map-v3/elements/planetOrbit/*`, `map-v3/useMapV3Trails.ts`, `scene/v3/layers/PlanetOrbitLayer.tsx`, `OrbitTrailV3.tsx`

---

## § Planet body

*Not implemented.*

---

## § Moon orbit

*Not implemented.*

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
| 2026-05-21 | Phase 2 | Planet orbits reuse v2 BodyOrbit geometry; v3 adds modular kind + `OrbitTrailV3`; phase 2 enables full solar camera bounds. |
