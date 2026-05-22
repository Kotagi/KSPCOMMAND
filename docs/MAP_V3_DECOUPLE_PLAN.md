# Map V3 decouple from Map V2 — complete

V3 is the **canonical** solar map foundation. V1/V2 remain selectable for regression; they **re-import** V3 core types where needed.

## Steps (all done)

| Step | Scope | Status |
|------|--------|--------|
| **1** | `MapContext` in `map-v3/MapContext.ts`; `map-v2/MapContext.ts` re-exports | Done |
| **2** | `SceneFrame` + `rootPointSafety` in `map-v3/`; v2 re-exports | Done |
| **3** | Planet orbits — `filterHeliocentricPlanetOrbit` + v3-native `buildPlanetOrbitSegments` (no `TrajectoryPlanner` in `map-v3/`) | Done |
| **4** | Program docs updated (this file, structure, modules, spec, program state) | Done |

## What “standalone V3” means

```text
Telemetry → map-v3/MapContext → map-v3/planner/buildSegments → map-v3/SceneFrame
         → scene/v3/layers/* → shared scene drawers (GradientDirectionalOrbitTrail, …)
```

| Import rule | Detail |
|-------------|--------|
| **`map-v3/` must not import `map-v2/TrajectoryPlanner`** | Enforced in production code; parity tests may import v2 for regression only |
| **`map-v2/MapContext` and `map-v2/SceneFrame`** | Thin re-exports from `map-v3` (dependency inverted) |
| **`scene/v3/`** | No `map-v2` imports |
| **Shared (OK)** | `coords/`, `telemetry/`, `model/`, `scene/CameraRig`, `scene/MoonVisibilityContext`, `scene/bodyMapColors`, etc. |

## New elements (Phase 3+)

Add under `map-v3/elements/<kind>/` + `planner/buildSegments.ts` + `scene/v3/layers/*`. **Do not** call `map-v2/TrajectoryPlanner` from `map-v3/`. Use v2 layers only as a **reference** when porting behavior.

## Verification

```powershell
cd web
npm test
npm run build
```

- **72** tests (includes v2/v3 planet-orbit path parity on Kerbin fixture).
- In flight: **3D Map V3** — `94-heliocentric-relative-unified` in console after `?v=94` refresh (see [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md) for DLL frame rules).

## Related docs

- [`MAP_V3_PROGRAM_STATE.md`](MAP_V3_PROGRAM_STATE.md) — as-built architecture
- [`MAP_V3_STRUCTURE.md`](MAP_V3_STRUCTURE.md) — repository tree
- [`MAP_V3_PLANET_ORBIT_SPEC.md`](MAP_V3_PLANET_ORBIT_SPEC.md) — planet orbit data path
