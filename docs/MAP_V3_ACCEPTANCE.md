# Map V3 — Acceptance thresholds

Compare **in-game KSP map** vs web **View → 3D Map V3** when elements are enabled. Ctrl+F5 / cache-bust after installing a new bundle.

## Phase 0 — Blank foundation

| ID | Pass criteria |
|----|----------------|
| P0-01 | View dropdown includes **3D Map V3**; selecting it sets `dataset.solarView` to `3d-v3` |
| P0-02 | Viewport shows dark background (`#071019`) and starfield; no bodies, orbits, vessel, or labels |
| P0-03 | No console errors on load or camera orbit |
| P0-04 | Recenter / camera modes do not throw (shared `CameraRig`) |
| P0-05 | V1 (`3d`) and V2 (`3d-v2`) unchanged |
| P0-06 | `npm run build` and `npm test` succeed |
| P0-07 | `MapHudV3` shows phase label **v3 phase 0 — blank** |

## Phase 1 — Primary star

| ID | Pass criteria |
|----|----------------|
| P1-01 | Textured emissive star visible at map center after Recenter (same save/UT as in-game KSP solar view) |
| P1-02 | Side-by-side with **3D Map V2** — star appearance acceptable |
| P1-03 | No orbits, planet dots, vessel, labels, or SOI rings |
| P1-04 | No console errors on load, orbit, or Recenter |
| P1-05 | `MapHudV3` shows **v3 phase 1 — star** |
| P1-06 | `buildStarMarkerSegments` Vitest: 1 segment, `bodyName === rootBody`, position near origin |
| P1-07 | `composeMapV3Layers(PHASE1)` === `["StarMarkerLayer"]` |

**Automated (2026-05-21):** P1-06, P1-07 pass via `npm test`. P1-01–P1-05 require manual flight check.

## Future phases

Per-element criteria added when the corresponding flag is enabled (mirror `docs/MAP_V2_ACCEPTANCE.md`).

## Automated (shared)

- `web/src/coords/*.test.ts` — shared math
- `MapComposer.test.ts` — phase 0 and phase 1 composition
