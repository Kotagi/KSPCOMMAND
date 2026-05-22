# Map V3 — Module contracts

Parallel modular 3D solar map (`solarRenderMode: 3d-v3`). V1 (`3d`) and V2 (`3d-v2`) are unchanged.

## Core (`web/src/map-v3/`)

| Module | Inputs | Outputs | Consumer |
|--------|--------|---------|----------|
| `MapContext.ts` | `TelemetrySnapshot` | `MapContext`, `starBody(ctx)` | Provider, anchors |
| `SceneFrame.ts` | focus, `displayScale` | `toScenePoint(root)` | Layers |
| `types.ts` | — | `MapElementKind`, `SystemAnchor`, `TrajectorySegment` | Planner + layers |
| `layerFlags.ts` | — | `MAP_V3_LAYERS_PHASE0/1/2` | `Map3DV3`, tests |
| `useMapV3Trails.ts` | `MapContext`, kind | root + scene trails | Orbit layers |
| `MapComposer.ts` | `MapV3LayerFlags` | active layer id list | Docs + tests |
| `MapV3Context.tsx` | store + moon LOD | React context | All v3 layers |
| `planner/buildSegments.ts` | `MapContext`, kind | `TrajectorySegment[]` | Layers |

## Element — `planetOrbit` (`web/src/map-v3/elements/planetOrbit/`)

| Module | Inputs | Outputs | Consumer |
|--------|--------|---------|----------|
| `planetOrbitStyle.ts` | `bodyName` | color + width constants | `OrbitTrailV3` |
| `buildPlanetOrbitSegments.ts` | `MapContext` | `TrajectorySegment[]` kind `planetOrbit` | Planner, layer |

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
| `layers/OrbitTrailV3.tsx` | Retro/prograde split `Line` trails |

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
3. Add `build*Segments` under `map-v3/elements/<kind>/`
4. Register case in `planner/buildSegments.ts`
5. Add `*Layer.tsx` under `scene/v3/layers/` and mount in `MapV3LayerStack.tsx`
6. Enable in next `MAP_V3_LAYERS_PHASEn` constant
7. Fill `MAP_V3_RENDERING_GUIDE.md` + `MAP_V3_ACCEPTANCE.md`
