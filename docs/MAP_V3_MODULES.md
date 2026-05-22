# Map V3 — Module contracts

Parallel modular 3D solar map (`solarRenderMode: 3d-v3`). **V3 core is canonical** ([`MAP_V3_DECOUPLE_PLAN.md`](MAP_V3_DECOUPLE_PLAN.md)); v2 re-exports `MapContext` / `SceneFrame` and keeps `TrajectoryPlanner` for `3d-v2` only.

## Core (`web/src/map-v3/`)

| Module | Inputs | Outputs | Consumer |
|--------|--------|---------|----------|
| `MapContext.ts` | `TelemetrySnapshot` | `MapContext`, `isPlanetBody`, `starBody` | Provider, all v3 builders |
| `SceneFrame.ts` | focus, `displayScale` | `toScenePoint`, `toScenePoints` | Layers |
| `rootPointSafety.ts` | `Vector3` | `isFiniteRootPoint` | `SceneFrame` |
| `types.ts` | — | `MapElementKind`, `SystemAnchor`, `TrajectorySegment` | Planner + layers |
| `layerFlags.ts` | — | `MAP_V3_LAYERS_PHASE0/1/2` | `Map3DV3`, tests |
| `useMapV3Trails.ts` | `MapContext`, kind | root + scene trails | Orbit layers |
| `MapComposer.ts` | `MapV3LayerFlags` | active layer id list | Docs + tests |
| `MapV3Context.tsx` | store + moon LOD | React context | All v3 layers |
| `planner/buildSegments.ts` | `MapContext`, kind | `TrajectorySegment[]` | Layers |

## Element — `planetOrbit` (`web/src/map-v3/elements/planetOrbit/`)

| Module | Inputs | Outputs | Consumer |
|--------|--------|---------|----------|
| `filterHeliocentricPlanetOrbit.ts` | `MapContext`, `BodyOrbitPath` | include/exclude heliocentric planet path | `buildPlanetOrbitSegments` |
| `planetOrbitStyle.ts` | `bodyName` | widths, `trailVertices` (**512**) | densify / layer |
| `densifyPlanetOrbitTrail.ts` | `BodyOrbitPath`, `MapContext` | samples-first points, densify, UT helpers | `buildPlanetOrbitSegments` |
| `buildPlanetOrbitSegments.ts` | `MapContext` | `TrajectorySegment[]` kind `planetOrbit` (v3-native, no v2 planner) | `planner/buildSegments`, layer |

## Element — `starMarker` (`web/src/map-v3/elements/starMarker/`)

| Module | Inputs | Outputs | Consumer |
|--------|--------|---------|----------|
| `resolveSystemAnchors.ts` | `MapContext` | `SystemAnchor[]` (P1: 0–1) | `buildStarMarkerSegments` |
| `buildStarMarkerSegments.ts` | `MapContext` | `TrajectorySegment[]` kind `starMarker` | Planner, layer |

## Presentation (`web/src/scene/v3/`)

| Module | Role |
|--------|------|
| `Map3DV3.tsx` | Canvas; `MAP_V3_LAYERS_PHASE2` |
| `MapV3LayerStack.tsx` | Mounts layer components |
| `layers/StarMarkerLayer.tsx` | Emissive textured sphere per segment |
| `layers/PlanetOrbitLayer.tsx` | Planet orbit polylines via `OrbitTrailV3` |
| `layers/OrbitTrailV3.tsx` | Planet trails → `GradientDirectionalOrbitTrail` |
| `scene/GradientDirectionalOrbitTrail.tsx` | Shared motion-tail drawer (single closed ring; split fallback) |
| `scene/splitOrbitTrailHalves.ts` | Half-orbit polyline split at anchor |
| `scene/orbitTrailDirectionStyle.ts` | Motion-tail opacity (`opacityForOrbitTailAhead`, attach 1.0 / lead 0.25) |

## Dev — planet orbit colors

| Module | Role |
|--------|------|
| `scene/kspBodyMapColorTable.ts` | Shipped `KSP_BODY_MAP_COLORS` |
| `scene/bodyMapColors.ts` | `getKspBodyMapColor`, `useKspBodyMapColor` |
| `settings/customizeMapDev.ts` | `localStorage` load/save for Customize Map |
| `components/CustomizeMapDevPanel.tsx` | HUD: enable, pick orbit, **Set color**, revert |
| `selection/pickPlanetOrbitTrail.ts` | Ray pick heliocentric planet rings |
| `store/viewStore.ts` | `planetOrbitColorOverrides`, `planetOrbitStockDefaults` |

Guide: [`PLANET_ORBIT_COLOR_GUIDE.md`](PLANET_ORBIT_COLOR_GUIDE.md)

## Element kinds

| Kind | Phase | Layer file | Status |
|------|-------|------------|--------|
| `starMarker` | 1 | `StarMarkerLayer.tsx` | **Implemented** |
| `planetOrbit` | 2 | `PlanetOrbitLayer.tsx` | **Implemented** |
| `planetBody` | 3 | `PlanetBodyLayer.tsx` | Planned |
| `moonOrbit` | 4 | `MoonOrbitLayer.tsx` | Planned |
| `moonBody` | 5 | `MoonBodyLayer.tsx` | Planned |
| `vesselMarker` | 6 | `VesselMarkerLayer.tsx` | Planned |
| `bodyLod` | 7 | (body layers) | Planned |
| `vesselOrbit` | 8 | `VesselOrbitLayer.tsx` | Planned |
| `bodyLabel` | 9 | `BodyLabelLayer.tsx` | Planned |
| `futureRoute` | 10 | `FutureRouteLayer.tsx` | Planned |
| `soiRing` | 11 | `SoiRingLayer.tsx` | Planned |
| `selection` | 12 | `SelectionLayer.tsx` | Planned |

## Phase 2 state

- `MAP_V3_LAYERS_PHASE2`: `starMarker` + `planetOrbit` true
- `composeMapV3Layers(PHASE2)` → `["StarMarkerLayer", "PlanetOrbitLayer"]`
- Production `Map3DV3` uses phase 2 flags
- `MAP_V3_LAYERS_PHASE1` retained for regression tests

## Multi-star (planned)

- `MapContext` remains **per snapshot**, keyed by `telemetry.rootBody` + `rootFrameName`.
- `resolveSystemAnchors` will return multiple `SystemAnchor` entries when telemetry supports alternate systems.
- `StarMarkerLayer` already maps all segments (one mesh per anchor).
- Planners and layers use `ctx.rootBody`, never literal `"Sun"`.
- Future store field (e.g. `activeSystemRoot`) selects which subtree to render.

## Adding a new map element (procedure)

1. Add kind to `MapElementKind` in `types.ts`
2. Add flag to `MapV3LayerFlags` in `layerFlags.ts`
3. Add `build*Segments` under `map-v3/elements/<kind>/` — **do not import `map-v2/TrajectoryPlanner`**
4. Register case in `planner/buildSegments.ts`
5. Add `*Layer.tsx` under `scene/v3/layers/` and mount in `MapV3LayerStack.tsx`
6. Enable in next `MAP_V3_LAYERS_PHASEn` constant
7. Fill `MAP_V3_RENDERING_GUIDE.md` + `MAP_V3_ACCEPTANCE.md`

Port behavior from v2 layers only as a reference; implement segment data in `map-v3/`.
