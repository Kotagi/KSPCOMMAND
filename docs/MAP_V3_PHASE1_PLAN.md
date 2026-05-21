# Map V3 — Phase 1 plan

**Goal:** Render the primary system star at the map center, with modular element pipeline ready for multi-star modes later.

## Deliverables

1. **Element module** — `web/src/map-v3/elements/starMarker/` (`resolveSystemAnchors`, `buildStarMarkerSegments`)
2. **Planner** — `web/src/map-v3/planner/buildSegments.ts` (dispatches `starMarker`)
3. **Layer** — `web/src/scene/v3/layers/StarMarkerLayer.tsx`
4. **Flags** — `MAP_V3_LAYERS_PHASE1` (`starMarker: true`); `Map3DV3` uses phase 1
5. **Docs** — rendering guide § Star, modules, acceptance Phase 1, structure rev 0.2

## Out of scope (Phase 1)

- Planet/moon orbits and bodies
- Multi-star UI or alternate root selection
- Star pick / selection highlight

## Implementation checklist

- [x] `SystemAnchor` type + `resolveSystemAnchors`
- [x] `buildStarMarkerSegments` + Vitest
- [x] `planner/buildSegments` for `starMarker`
- [x] `StarMarkerLayer` + `MapV3LayerStack`
- [x] `MAP_V3_LAYERS_PHASE1` wired in `Map3DV3`
- [x] `npm test` / `npm run build`
- [ ] Manual in-game compare with KSP map (see acceptance)

## Test plan

1. `cd web && npm test && npm run build`
2. `npm run dev` → View **3D Map V3** → orange/textured sphere at center; HUD **v3 phase 1 — star**
3. Recenter → star remains framed (system focus)
4. `.\scripts\build.ps1` → in-game `8750` hard-refresh → same check
5. Side-by-side **3D Map V2** — star appearance comparable

## Next phase (preview)

**Phase 2:** `planetOrbit` → `PlanetOrbitLayer` + `elements/planetOrbit/`.
