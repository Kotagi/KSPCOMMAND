# Map V3 — Phase 0 plan

**Goal:** Foundation only — blank 3D map, modular folder layout, documentation hooks. No map objects.

## Deliverables

1. **Structure spec** — `docs/MAP_V3_STRUCTURE.md` (folder tree, principles, interfaces)
2. **Living docs** — `MAP_V3_MODULES.md`, `MAP_V3_RENDERING_GUIDE.md`, `MAP_V3_ACCEPTANCE.md`
3. **Code scaffold** — `web/src/map-v3/`, `web/src/scene/v3/`
4. **UI** — View → **3D Map V3** (`3d-v3`), `MapHudV3`, `SolarMapPanel` mount
5. **Blank scene** — Canvas, lights, background color, starfield, `CameraRig`, no element layers

## Out of scope (Phase 0)

- Trajectory planner / segment builders under `map-v3/elements/`
- Any `scene/v3/layers/*Layer.tsx` implementation
- Telemetry-driven drawing
- GameData bundle publish (run build locally for test)

## Implementation checklist

- [x] `SolarRenderMode` includes `"3d-v3"`
- [x] `Map3DV3` + `MapV3Provider` + error boundary + scene effects
- [x] `MAP_V3_LAYERS_PHASE0` all false; `MapComposer` returns no layers
- [x] HUD phase strip for v3
- [x] Manual test in browser (see acceptance)
- [x] `npm run build` passes
- [x] `npm test` passes

## Test plan (end of Phase 0)

1. `cd web && npm run build && npm test`
2. `.\scripts\build.ps1` (optional: refresh `GameData/.../ksp-solar-map.js` for in-game CEF)
3. Open dashboard / `http://127.0.0.1:8750/` with telemetry connected
4. View → **3D Map V3**
5. Expect: dark viewport, starfield, orbit camera works, no meshes/lines/labels
6. Console: no errors
7. Switch to V1 and V2: still load
8. `document.body.dataset.solarView === "3d-v3"`

## Next phase

**Phase 1:** See [`MAP_V3_PHASE1_PLAN.md`](MAP_V3_PHASE1_PLAN.md) — primary star shipped.
