# Map V3 — Structure specification (Phase 0 baseline)

**Document ID:** MAP-V3-STRUCT-001  
**Status:** Phase 2 — `starMarker` + `planetOrbit` shipped; V3 is the **canonical** map for new work  
**Scope:** Repository layout, module boundaries, and rollout gates for the modular 3D solar map (`solarRenderMode: 3d-v3`, default).  
**Reference:** Stock KSP in-game map (acceptance), Map V2 lessons (`docs/MAP_V2_MODULES.md`), orbit trails ([`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md)).

---

## 1. Purpose

Map V3 is a **stepwise, modular** replacement path for V2: same telemetry and shared math, but each visual object type lives in an isolated **element module** with a documented draw contract. Phase 0 delivers only:

- Selectable **3D Map V3** view in the web UI
- Empty WebGL scene (background, lighting, starfield, camera)
- Folder scaffold and living documentation (`MAP_V3_RENDERING_GUIDE.md`, `MAP_V3_MODULES.md`)

No bodies, orbits, vessels, or labels are rendered in Phase 0.

---

## 2. Design principles

| ID | Principle | Rationale |
|----|-----------|-----------|
| P-01 | **One element kind → one layer file** | Traceability from KSP map feature to code and docs |
| P-02 | **Planner produces data; layer only draws** | `TrajectoryPlanner` / segment builders stay free of Three.js |
| P-03 | **SceneFrame is the only coordinate gate** | Root meters → scene units in one place (`SceneFrame.ts`) |
| P-04 | **Layer flags gate rollout** | Enable one phase at a time; blank map = all flags false |
| P-05 | **No hardcoded stock body lists** | Hierarchy from telemetry snapshot |
| P-06 | **V1 and V2 unchanged** | `3d` and `3d-v2` remain selectable and testable |
| P-07 | **Document as we ship** | Each enabled element updates `MAP_V3_RENDERING_GUIDE.md` |

---

## 3. Repository tree (authoritative)

```
web/src/
├── map-v3/                          # Domain: telemetry → segments → scene frame (no React Three)
│   ├── types.ts                     # MapElementKind, TrajectorySegment, SceneFrameState
│   ├── layerFlags.ts                # Per-element enable flags + phase labels
│   ├── MapContext.ts                # Snapshot → bodies, hierarchy, UT (reuses v2 builder in P0)
│   ├── SceneFrame.ts                # Root → scene coordinates (reuses v2 in P0)
│   ├── MapV3Context.tsx             # React context: mapContext, sceneFrame, layers
│   ├── MapComposer.ts               # Pure: which layer ids are active for current flags
│   ├── planner/buildSegments.ts     # Dispatches MapElementKind → element builders
│   └── elements/                    # One folder per MapElementKind
│       ├── README.md
│       └── starMarker/              # resolveSystemAnchors, buildStarMarkerSegments
│
├── scene/v3/                        # Presentation: R3F Canvas and layers only
│   ├── Map3DV3.tsx                  # Canvas root, provider stack, layer composition
│   ├── MapV3SceneEffects.tsx        # Background stars (no postprocessing in CEF)
│   ├── MapV3SceneErrorBoundary.tsx  # In-tree error surface
│   └── layers/                      # One TSX per MapElementKind
│       └── StarMarkerLayer.tsx      # Phase 1
│
├── components/
│   ├── MapHudV3.tsx                 # Phase / status strip when 3d-v3 selected
│   ├── MapHud.tsx                   # View selector includes 3d-v3
│   └── SolarMapPanel.tsx            # Mounts Map3DV3 when mode = 3d-v3
│
└── store/viewStore.ts               # Default solarRenderMode "3d-v3"

web/src/scene/                       # Shared presentation (orbit drawer used by v3)
├── GradientDirectionalOrbitTrail.tsx
├── splitOrbitTrailHalves.ts
└── orbitTrailDirectionStyle.ts

docs/
├── README.md                        # Documentation index
├── MAP_V3_STRUCTURE.md              # This file
├── MAP_V3_PROGRAM_STATE.md          # As-built program state
├── ORBIT_TRAIL_DRAWING_GUIDE.md     # Orbit trail manual (read before editing trails)
├── MAP_V3_PHASE0_PLAN.md            # Phase 0 scope and test plan
├── MAP_V3_MODULES.md                # Module I/O contracts (living)
├── MAP_V3_RENDERING_GUIDE.md        # How each object type is drawn (living)
└── MAP_V3_ACCEPTANCE.md             # Per-phase pass criteria
```

**Out of scope for V3 tree (shared):** `web/src/coords/`, `web/src/telemetry/`, `web/src/camera/`, `web/src/scene/CameraRig.tsx`, `web/src/scene/MoonVisibilityContext.tsx` — consumed by V3, not duplicated.

---

## 4. Layer naming convention

| Suffix | Location | Responsibility |
|--------|----------|----------------|
| `*Layer.tsx` | `scene/v3/layers/` | R3F component: reads `useMapV3()`, renders geometry |
| `build*Segments` | `map-v3/elements/<kind>/` (future) | Pure segment list for planner |
| `MAP_V3_LAYERS_*` | `map-v3/layerFlags.ts` | Feature flags |

File name = PascalCase layer matching `MapElementKind` (e.g. `StarMarker` → `StarMarkerLayer.tsx`).

---

## 5. Data flow (target end state)

```mermaid
flowchart LR
  T[TelemetrySnapshot] --> MC[MapContext]
  MC --> TP[TrajectoryPlanner / element builders]
  TP --> SEG[TrajectorySegment[]]
  SEG --> SF[SceneFrame.toScenePoint]
  SF --> L[scene/v3/layers/*]
  L --> GL[WebGL via R3F]
```

Phase 0 stops after `MapV3Context` (no segments, no layers).

---

## 6. Interface summary

### 6.1 `MapContext` (from telemetry)

- **Inputs:** `TelemetrySnapshot | null`
- **Outputs:** `rootBody`, `hierarchy`, `bodyByName`, positions at UT, `canDraw`
- **Phase 0:** Delegates to `map-v2/MapContext.ts` (identical contract)

### 6.2 `SceneFrame`

- **Inputs:** focus mode, `displayScale`, focus position
- **Outputs:** `toScenePoint(rootMeters)` → scene units
- **Phase 0:** Types in `map-v3/types.ts`; transform reuses v2 implementation

### 6.3 `MapV3LayerFlags`

- Boolean per `MapElementKind`
- **Phase 0:** `MAP_V3_LAYERS_PHASE0` — all `false`

### 6.4 `MapComposer` vs `MapV3LayerStack`

- **`composeMapV3Layers(flags)`** — **Inputs:** `MapV3LayerFlags`; **Outputs:** ordered layer component ids (Vitest + docs).
- **`MapV3LayerStack.tsx`** — manually mounts layers for the active phase constant; not yet driven by `composeMapV3Layers` at runtime (intentional in phase 2).

---

## 7. Phase gate matrix (planned)

| Phase | Flag(s) enabled | Layer(s) | Doc section |
|-------|-----------------|----------|-------------|
| 0 | none | — | Structure + blank scene |
| 1 | `starMarker` | `StarMarkerLayer` | § Star — **shipped** |
| 2 | `planetOrbit` | `PlanetOrbitLayer` | § Planet orbit |
| 3 | `planetBody` | `PlanetBodyLayer` | § Planet body |
| … | … | … | … |

Full matrix will mirror V2 roles (`docs/MAP_V2_MODULES.md`) but with V3 element IDs and independent layer files.

---

## 8. UI integration

| Surface | Behavior |
|---------|----------|
| View dropdown | Option **3D Map V3** → `solarRenderMode = "3d-v3"` |
| `document.body.dataset.solarView` | `"3d-v3"` for CSS hooks |
| HUD | `MapHudV3` shows phase label and root body (telemetry only) |
| Fullscreen | Same as V1/V2 (panel portal) |

---

## 9. Traceability

| Requirement | Verification |
|-------------|--------------|
| V3 selectable | Manual: View → 3D Map V3 |
| Blank scene | No layer components mounted; dark `#071019` + stars |
| V1/V2 regression | View → 3D WebGL / 3D Map V2 unchanged |
| Modular docs | `MAP_V3_RENDERING_GUIDE.md` table exists per element kind |
| Build | `web` `npm run build` + `scripts/build.ps1` |

---

## 11. Multi-star extension (planned)

| Piece | Phase 1 | Future |
|-------|---------|--------|
| `SystemAnchor` | One entry from `starBody(ctx)` via `resolveSystemAnchors` | N entries per telemetry mode / subtree |
| Planner | `buildStarMarkerSegments` maps anchors → segments | Same; no layer rewrite |
| Layer | `segments.map` → one mesh per anchor | Same pattern |
| Store | Uses `telemetry.rootBody` only | `activeSystemRoot` or per-system snapshot |
| Focus | `system` → star at scene origin | `star(name)` shifts `SceneFrame` focus |

Never use literal `"Sun"` in v3 code; use `ctx.rootBody` and segment `bodyName`.

---

## 10. Revision history

| Rev | Date | Change |
|-----|------|--------|
| 0.1 | 2026-05-21 | Initial structure spec and Phase 0 scaffold |
| 0.2 | 2026-05-21 | Phase 1 star element, planner, `StarMarkerLayer`, multi-star §11 |
