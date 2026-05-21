# Map V2 — Module contracts

Parallel 3D solar map (`solarRenderMode: 3d-v2`). v1 (`3d`) is unchanged. Acceptance reference is the **stock KSP in-game map**, not v1 web rendering.

## Core (`web/src/map-v2/`)

| Module | Inputs | Outputs | Owner layer |
|--------|--------|---------|-------------|
| `MapContext.ts` | `TelemetrySnapshot` | `rootBody`, hierarchy, `bodyByName`, positions at UT | All |
| `SceneFrame.ts` | `MapContext`, `displayScale`, `displayFocus` (from moon LOD) | `toScenePoint(root)` | All |
| `TrajectoryPlanner.ts` | `MapContext`, `TrajectoryRole`, filters | `TrajectorySegment[]` root-frame polylines | Per role below |
| `BodyRepresentation.ts` | camera distance, mesh radius | `mesh` \| `icon` LOD | `PlanetBodyLayer`, `MoonBodyLayer` |
| `layerFlags.ts` | — | enabled layer toggles | `Map3DV2` |

## Trajectory roles

| Role | Phase | Layer | KSP analogue |
|------|-------|-------|----------------|
| `StarMarker` | 1 | `StarLayer` | Star at system origin |
| `BodyOrbit` (planets) | 2 | `PlanetOrbitLayer` | Heliocentric colored trails |
| `BodyPosition` (planets) | 3 | `PlanetBodyLayer` | Planet on trail at UT |
| `BodyOrbit` (moons) | 4 | `MoonOrbitLayer` | Moon rings around parent |
| `BodyPosition` (moons) | 5 | `MoonBodyLayer` | Moon on trail |
| `VesselPosition` | 6 | `VesselMarkerLayer` | Ship icon |
| `ActiveVesselLeg` | 8 | `VesselOrbitLayer` | Transfer arc to encounter |
| `FutureRouteLeg` | 10 | `FutureRouteLayer` | Yellow post-encounter legs |
| `SoiRing` | 11 | `SoiLayerV2` | SOI wireframes when zoomed |
| `BodyLabel` | 9 | `BodyLabelsLayer` | Name near body when close |

Planner reuses `buildBodyOrbitTrail`, `buildPatchConic` (display patch policy: heliocentric → Sun elliptic with encounter).

## Data rules

- No hardcoded stock body lists for geometry.
- Positions: `bodies[].positionRootRelativeMeters` at `gameUniversalTimeSeconds`.
- Colors: `getKspBodyMapColor` + generated fallback.

## Future systems (§ multi-star)

- `MapContext` is **per snapshot**, keyed by `telemetry.rootBody` + `rootFrameName`.
- `SceneFrame` focus: `system` (root origin) or `body(name)` / future `star(name)`.
- Planners use `context.rootBody`, never literal `"Sun"` for anchors.
- Multiple stars later: one `Map3DV2` + camera state per selected snapshot subtree.

## Adding a new map element

1. Add `TrajectoryRole` in `types.ts`.
2. Implement `buildSegments` case in `TrajectoryPlanner.ts`.
3. Add thin layer under `web/src/scene/v2/layers/`.
4. Enable flag in `layerFlags.ts` and compose in `Map3DV2.tsx`.
5. Document row in this file + threshold in `MAP_V2_ACCEPTANCE.md`.
