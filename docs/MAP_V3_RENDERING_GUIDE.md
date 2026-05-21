# Map V3 — Rendering guide (living document)

How each **object type** is drawn on the modular map. Update a section when that element’s layer ships.

**Status:** Phase 1 — `starMarker` implemented; other elements not rendered.

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
| `planetOrbit` | `PlanetOrbitLayer` | 2 | Not started | § Planet orbit |
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

| Field | Value |
|-------|-------|
| Data source | TBD — `bodyOrbitPaths` / analytic trail |
| Primitive | TBD — `Line` / `Line2` closed polyline |
| Color | TBD — `getKspBodyMapColor` |

*Not implemented.*

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
