# Map V3 — Program state (as-built)

**Revision:** 2026-05-21 (post phase 2 + motion-tail orbit opacity)

---

## Executive summary

| Phase | Status |
|-------|--------|
| 0 — Blank canvas | Complete |
| 1 — Star marker | Complete |
| 2 — Planet orbits | Complete (KSP motion tail on single closed ring, `81-orbit-motion-tail`) |
| 3–12 | Not started (see [`MAP_V3_MODULES.md`](MAP_V3_MODULES.md)) |

- **Default view:** `solarRenderMode: "3d-v3"` in `web/src/store/viewStore.ts`.
- **New feature work** targets V3 paths; v1/v2 remain for regression only.

---

## Architecture

```mermaid
flowchart TB
  subgraph data [Data plane - map-v3]
    T[TelemetrySnapshot]
    MC[MapContext]
    BS[buildPlanetOrbitSegments / buildStarMarkerSegments]
    SEG[TrajectorySegment]
    T --> MC --> BS --> SEG
  end
  subgraph presentation [Presentation - scene/v3]
    SF[SceneFrame / useV3SceneTrails]
    L[MapV3LayerStack layers]
    GL[Map3DV3 Canvas]
    SEG --> SF --> L --> GL
  end
  subgraph shared [Shared with v2]
    TP[TrajectoryPlanner BodyOrbit]
    COLORS[bodyMapColors / buildBodyOrbitTrail]
  end
  MC --> TP
  BS --> TP
  BS --> COLORS
```

---

## Element matrix

| Kind | Builder | Layer | Status |
|------|---------|-------|--------|
| `starMarker` | `buildStarMarkerSegments` | `StarMarkerLayer` | Shipped |
| `planetOrbit` | `buildPlanetOrbitSegments` | `PlanetOrbitLayer` → `OrbitTrailV3` | Shipped |
| `planetBody` | — | `PlanetBodyLayer` | Planned phase 3 |
| `moonOrbit` | — | `MoonOrbitLayer` | Phase 4 |
| `moonBody` | — | `MoonBodyLayer` | Phase 5 |
| `vesselMarker` | — | `VesselMarkerLayer` | Phase 6 |
| `vesselOrbit` | — | `VesselOrbitLayer` | Phase 8 |
| `bodyLabel` | — | `BodyLabelLayer` | Phase 9 |
| `futureRoute` | — | `FutureRouteLayer` | Phase 10 |
| `soiRing` | — | `SoiRingLayer` | Phase 11 |
| `selection` | — | `SelectionLayer` | Phase 12 |

---

## Shared dependencies on v2

| V2 module | V3 usage |
|-----------|----------|
| `map-v2/MapContext.ts` | `buildMapContext` (phase 0–2) |
| `map-v2/TrajectoryPlanner` `BodyOrbit` | Planet path geometry before v3 densify |
| `map-v2/SceneFrame` | Re-exported / reused for `toScenePoints` |
| `coords/buildBodyOrbitTrail.ts` | Analytic planet rings |
| `scene/GradientDirectionalOrbitTrail.tsx` | Shared trail drawer (not v3-exclusive) |

---

## Known debt / intentional shortcuts

| Item | Notes |
|------|-------|
| `MapV3LayerStack.tsx` | Manual layer list; `composeMapV3Layers` used in tests/docs only — dynamic registry deferred |
| `planetBody` | Spec’d but not rendered |
| Vessel orbits | Documented in orbit guide; not wired to drawer |
| `trailDrawSegments.ts` | Open-trail prototype, unused in production |
| Analytic planet rings | No `sampleUniversalTimes`; UT wiring applies to sample-only paths |

---

## Orbit trail stack (phase 2 detail)

- Drawer: `GradientDirectionalOrbitTrail` — one closed `Line` for planet rings.
- Style: `orbitTrailDirectionStyle.ts` — `opacityForOrbitTailAhead`, `closedRingHalfGradientOpacities` (attach **1.0**, lead **0.25**, linear ramp in prograde order).
- Split fallback: `splitOrbitTrailHalves.ts` (open / duplicate-endpoint paths only).
- Colors: stock `bodyMapColors.ts` palette; alpha-only fade, fixed hue.
- Visual spec: [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) §2 (motion tail).

---

## Forward path

Phases 3–12 follow [`MAP_V3_MODULES.md`](MAP_V3_MODULES.md) and phase plans (`MAP_V3_PHASE*_PLAN.md`). Each phase enables one `MapV3LayerFlags` group, adds `build*Segments`, layer TSX, rendering guide section, and acceptance rows.

---

## Verification baseline

- `npm test` / `npm run build` green.
- UI version: `KSP_WEB_MAP_UI_VERSION` in `web/src/mount.tsx` (bumped on each install pass).
- Manual: [`MAP_V3_ACCEPTANCE.md`](MAP_V3_ACCEPTANCE.md) phase 2 rows P2-01–P2-10.
