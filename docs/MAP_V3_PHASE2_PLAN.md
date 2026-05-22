# Map V3 — Phase 2 plan

**Goal:** Heliocentric planet orbit polylines on **3D Map V3**, matching v1/v2 KSP colors and retrograde/prograde split styling. No moons, bodies, or vessel.

## Deliverables

1. **Spec** — `docs/MAP_V3_PLANET_ORBIT_SPEC.md`
2. **Element** — `web/src/map-v3/elements/planetOrbit/` + `useMapV3Trails.ts`
3. **Layers** — `OrbitTrailV3`, `PlanetOrbitLayer`, `MapV3LayerStack`
4. **Flags** — `MAP_V3_LAYERS_PHASE2`; `Map3DV3` uses phase 2
5. **Docs** — rendering guide § Planet orbit, modules, acceptance Phase 2

## Out of scope (Phase 2)

- `planetBody`, `moonOrbit`, `moonBody`, `vesselMarker`, `vesselOrbit`
- Orbit line pick/hover (Phase 12)
- Mod-specific color tables beyond `getKspBodyMapColor` fallback

## Implementation checklist

- [x] `planetOrbitStyle` + `buildPlanetOrbitSegments` + Vitest
- [x] `planner/buildSegments` for `planetOrbit`
- [x] `OrbitTrailV3` + `PlanetOrbitLayer` + `MapV3LayerStack`
- [x] `MAP_V3_LAYERS_PHASE2` wired in `Map3DV3` + `CameraRig` star-only guard
- [x] Documentation updates
- [x] `npm test` / `npm run build`
- [ ] Manual in-game compare with KSP map / v1 (see acceptance)

## Test plan

1. `cd web && npm test && npm run build`
2. `npm run dev` → **3D Map V3** → colored planet rings; no moons; HUD **v3 phase 2 — planet orbits**
3. Side-by-side **3D WebGL (v1)** — comparable colors
4. Recenter → full solar framing (not star-only)

## Next phase (preview)

**Phase 3:** `planetBody` — planet meshes positioned on orbit trails.
