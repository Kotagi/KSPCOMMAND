# Map V3 — Phase 3 guide (planet bodies + textures)

**Audience:** Operators testing in KSP, and developers extending Map V3.  
**Status:** Phase 3.1 (mesh/icon bodies) + Phase 3.3 (plugin-export textures) — **complete**.  
**UI build:** `107-planet-texture-material-ref` — hard-refresh `http://127.0.0.1:8750/?v=107`

| Doc | Role |
|-----|------|
| **This guide** | How-to, workflows, troubleshooting |
| [`MAP_V3_PLANET_BODY_SPEC.md`](MAP_V3_PLANET_BODY_SPEC.md) | Formal body element spec (LOD, position) |
| [`MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](MAP_V3_PLANET_BODY_TEXTURE_SPEC.md) | Formal texture export spec (plugin + HTTP) |
| [`MAP_V3_ACCEPTANCE.md`](MAP_V3_ACCEPTANCE.md) | Pass/fail checklists P3-xx, P3T-xx |
| [`web/dev/README.md`](../web/dev/README.md) | Planet texture lab only |

---

## 1. What Phase 3 delivers

Phase 3 adds **heliocentric planet markers** on top of Phase 1 (Sun) and Phase 2 (orbit rings):

| Feature | Zoomed out | Zoomed in (mesh LOD) |
|---------|------------|----------------------|
| **Position** | On the colored orbit ring (same authority as trails) | Same |
| **Appearance** | Fixed-size **round dot**, orbit color | **Sphere** at physical scale |
| **Texture (3.3)** | Dots stay **flat color** (no JPEG) | **Exported ScaledSpace JPEG** from the plugin |

Moons, vessels, labels, and SOI are **not** Phase 3 — see [`MAP_V3_MODULES.md`](MAP_V3_MODULES.md).

```mermaid
flowchart LR
  subgraph ksp [KSP flight]
    Bodies[FlightGlobals.Bodies]
    Export[BodyTextureExportService]
    JPEG[Web/assets/bodies/Name.jpg]
    Tel[Telemetry JSON]
    Bodies --> Export --> JPEG
    Bodies --> Tel
  end
  subgraph web [Browser Map V3]
    MC[MapContext]
    LOD[planetBodyLod]
    Tex[TexturedPlanetBody]
    Tel --> MC --> LOD
    JPEG --> HTTP[GET /assets/bodies/]
    HTTP --> Tex
  end
```

---

## 2. Prerequisites

| Requirement | Notes |
|-------------|-------|
| KSP 1.12.3 + matching mod DLL | Built with `scripts/build.ps1`, installed with `scripts/install.ps1` |
| `KSP_ROOT` set | Required for DLL build |
| Node.js + `npm` | For web bundle (`web/npm run build`) |
| Flight save loaded | Texture export runs in **flight**, not main menu |
| Web map | View → **3D Map V3** (`3d-v3`) |

---

## 3. Quick start (operator)

1. **Build and install** (see §8).
2. Start KSP, **load a flight** (any stock system save).
3. Wait a few seconds — KSP log should show `[KspWebMap] body texture Kerbin: ready` (and other planets).
4. Open **`http://127.0.0.1:8750/?v=107`** (Ctrl+F5).
5. Choose **View → 3D Map V3**, click **Recenter**.
6. **Zoom in** on Kerbin (or any planet) until the dot becomes a **sphere** — you should see the exported surface texture.
7. **Zoom out** — planets return to **small colored dots** on their orbit lines.

**HUD:** `v3 phase 3.3 — planet textures`  
**Console:** `[KspWebMap] UI 107-planet-texture-material-ref`

---

## 4. How a planet appears on the map

You do **not** register planets manually in the web UI. Inclusion is **automatic** from telemetry.

### 4.1 Which bodies count as “planet”

A body is drawn when **all** of the following hold:

| Rule | Source |
|------|--------|
| Listed in `hierarchy.planetNames` | Built from `telemetry.bodies[]` parent chain |
| Not the root body (Sun) | `name !== rootBody` |
| Has `positionRootRelativeMeters` | Valid snapshot |
| Visible in moon-LOD filter | `visibleBodyNames` (same as orbits) |

**Heliocentric rule (DLL + web):** `orbit.referenceBody === Sun` (root). Moons (parent = Kerbin, etc.) are **excluded**.

**Modded star systems:** Any body the DLL treats as a direct child of `rootBody` in the hierarchy is included — no hardcoded stock name list in the web planner.

### 4.2 Pipeline (code path)

```text
telemetry.bodies[]
  → buildMapContext() → hierarchy.planetNames, bodyByName
  → buildPlanetBodySegments() → one segment per planet (position point)
  → PlanetBodyLayer → PlanetBodyMesh
       → resolvePlanetBodyDrawMode() → "icon" | "mesh"
       → PlanetBodyDot | TexturedPlanetBody | FlatPlanetBody
```

**Files:** [`web/src/map-v3/elements/planetBody/buildPlanetBodySegments.ts`](../web/src/map-v3/elements/planetBody/buildPlanetBodySegments.ts), [`web/src/scene/v3/layers/PlanetBodyLayer.tsx`](../web/src/scene/v3/layers/PlanetBodyLayer.tsx)

### 4.3 Adding a “new planet” (mod / custom system)

**In KSP:** Install the planet mod; load a save where that body orbits the Sun.

**In the mod repo:** Usually **no web change** — if telemetry lists the body as a Sun child with position + radius, it appears automatically.

**Optional — orbit/body color:** Add an entry in [`web/src/scene/kspBodyMapColorTable.ts`](../web/src/scene/kspBodyMapColorTable.ts) or use **Customize Map** dev HUD ([`PLANET_ORBIT_COLOR_GUIDE.md`](PLANET_ORBIT_COLOR_GUIDE.md)). Unknown bodies get a default grey.

**Future — manual inclusion override:** Not implemented; do not hardcode body name lists in V3 planners ([`MAP_V3_DECOUPLE_PLAN.md`](MAP_V3_DECOUPLE_PLAN.md)).

---

## 5. Mesh vs icon (LOD)

Controlled by [`planetBodyLod.ts`](../web/src/map-v3/elements/planetBody/planetBodyLod.ts) (always on in V3; no separate layer flag).

| Mode | When | Visual |
|------|------|--------|
| **icon** | Projected mesh diameter ≤ **7 px** on screen | Round dot, orbit color, fixed screen size |
| **mesh** | Projected mesh diameter **> 7 px** | 3D sphere, radius from `bodyMeshRadius` |

**Dev override:** Customize Map → planet body LOD panel (`devPlanetBodyLodOverride`: auto / force icon / force mesh).

**Crossover tuning:** `PLANET_BODY_DOT_PIXEL_SIZE` in `planetBodyLod.ts`.

---

## 6. Planet textures (Phase 3.3)

### 6.1 Overview

Textures are **not** bundled in git (Squad/mod license). The **plugin exports** them at flight load from each planet’s in-game **ScaledSpace** material, then the **browser loads** them over HTTP when drawing **mesh LOD only**.

### 6.2 Export (automatic in KSP)

| Step | What happens |
|------|----------------|
| 1 | Flight loads → `BodyTextureExportService` starts |
| 2 | One planet per frame → read `CelestialBody.scaledBody` material |
| 3 | Blit albedo → resize max 1024 → JPEG quality 85 |
| 4 | Write `GameData/KspWebMap/Web/assets/bodies/{BodyName}.jpg` |
| 5 | Write `{BodyName}.jpg.meta` (material fingerprint for invalidation) |

**Re-export:** Only if fingerprint changes (texture mod) or JPEG missing — not every telemetry tick.

**Log examples:**

```text
[KspWebMap] body texture Kerbin: ready (42ms).
[KspWebMap] body texture export finished in 2.31s for 8 planets.
```

**DLL sources:** [`src/KspWebMap/Textures/ScaledBodyTextureExporter.cs`](../src/KspWebMap/Textures/ScaledBodyTextureExporter.cs), [`src/KspWebMap/Services/BodyTextureExportService.cs`](../src/KspWebMap/Services/BodyTextureExportService.cs)

### 6.3 On-disk layout (local only, gitignored)

```text
GameData/KspWebMap/Web/assets/bodies/
  Kerbin.jpg
  Kerbin.jpg.meta
  Duna.jpg
  ...
  README.md          ← committed (instructions only)
  .gitkeep           ← committed
```

See [`GameData/KspWebMap/Web/assets/bodies/README.md`](../GameData/KspWebMap/Web/assets/bodies/README.md).

### 6.4 Telemetry fields (schema v9)

On each **heliocentric planet** in `bodies[]`:

| Field | Example | Meaning |
|-------|---------|---------|
| `bodyTextureStatus` | `ready` | `pending` \| `ready` \| `failed` \| `unsupported` |
| `bodyTextureUrl` | `/assets/bodies/Kerbin.jpg?rev=4f6cdfba` | Browser load path (+ cache bust) |
| `bodyTextureRevision` | `4f6cdfba` | Hash of JPEG bytes |

**Check:** `http://127.0.0.1:8750/api/telemetry` → find `Kerbin` under `bodies[]`.

**Script:** `scripts/verify-telemetry.ps1` prints a texture status table for Sun-child planets.

### 6.5 How the web loads a texture on a planet

Automatic when **both** are true:

1. **LOD** = `mesh` (zoomed in enough).
2. **`bodyTextureStatus === "ready"`** and `bodyTextureUrl` is set.

**Flow:**

```text
PlanetBodyLayer passes bodyTexture* from telemetry
  → PlanetBodyMesh
       → isPlanetBodyTextureReady() ?
            yes → TexturedPlanetBody
              → loadBodyTexture(url, revision)
              → MeshBasicMaterial (map + white color, toneMapped: false)
            no  → FlatPlanetBody (orbit color sphere)
```

**Loader:** [`web/src/assets/planetBodyTextures.ts`](../web/src/assets/planetBodyTextures.ts) — cache keyed by URL + revision; `flipY = false` for KSP maps.

**Important:** Icon LOD **never** loads JPEGs (by design).

### 6.6 Manual checks (no DevTools required)

| Check | URL / action |
|-------|----------------|
| JPEG exists | Open `http://127.0.0.1:8750/assets/bodies/Kerbin.jpg` in browser |
| Telemetry | `http://127.0.0.1:8750/api/telemetry` — `bodyTextureStatus: "ready"` |
| Lab isolate | `http://127.0.0.1:8750/planet-texture-lab.html?textureUrl=/assets/bodies/Kerbin.jpg` |
| UI version | Browser console: `107-planet-texture-material-ref` |

---

## 7. Planet texture lab (developer tool)

Isolated full-screen sphere for testing **texture binding** without the full dashboard. **Not** part of `ksp-solar-map.js` (separate `planet-texture-lab.js` entry).

| Environment | URL |
|-------------|-----|
| **In-game HTTP** | `http://127.0.0.1:8750/planet-texture-lab.html?textureUrl=/assets/bodies/Kerbin.jpg` |
| **Vite dev** | `npx vite web/dev/planet-texture-lab.html` (proxies `/assets/bodies` to :8750 if KSP running) |

**Query params:**

| Param | Default | Purpose |
|-------|---------|---------|
| `textureUrl` | `/assets/bodies/Kerbin.jpg` | Path to JPEG |
| `rev` | (none) | Must match telemetry revision to test cache bust |

**Expected:**

| Look | Meaning |
|------|---------|
| Blue/green Kerbin | OK |
| Teal sphere | Loading / no URL |
| Flat white | Map not bound (regression) |
| Flat orbit color | Fallback (`status` not `ready`) |

Full detail: [`web/dev/README.md`](../web/dev/README.md).

---

## 8. Build and install

### 8.1 One-shot (recommended)

```powershell
$env:KSP_ROOT = "C:\Path\To\Kerbal Space Program"
.\scripts\build.ps1
.\scripts\install.ps1
```

This builds the DLL, runs `npm run build`, copies web assets + `planet-texture-lab.html` into `GameData/KspWebMap/`.

### 8.2 Web-only refresh

```powershell
cd web
npm run build
```

Copy `web/dist/assets/*` → `GameData/KspWebMap/Web/assets/` in your KSP install, and bump `?v=` in `index.html` if needed.

### 8.3 After install

1. Restart KSP (or reload flight) so the new DLL loads.
2. Hard-refresh browser (`?v=107`).

---

## 9. Verification

### 9.1 Automated

```powershell
cd web
npm test          # 86+ tests including planetBodyLod, planetBodyTextures
npm run build

.\scripts\verify-telemetry.ps1   # KSP running, flight loaded
```

### 9.2 Manual acceptance

| Section | Doc |
|---------|-----|
| Bodies + LOD | [`MAP_V3_ACCEPTANCE.md`](MAP_V3_ACCEPTANCE.md) § Phase 3.1 (P3-01–P3-14) |
| Textures | [`MAP_V3_ACCEPTANCE.md`](MAP_V3_ACCEPTANCE.md) § Phase 3.3 (P3T-01–P3T-10) |

### 9.3 Side-by-side with KSP

Use KSP **map view** vs web **3D Map V3** on the same flight: planet on ring, zoom icon↔mesh, mesh texture vs tracking map ScaledSpace.

---

## 10. Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| No planets at all | Phase 0–2 only / wrong view | View → **3D Map V3**; check `MAP_V3_LAYERS_PHASE3` |
| Dots only, never mesh | Zoomed out | Zoom in on planet; or dev HUD → force mesh |
| Mesh but flat **color** only | Textures not exported or `status` not `ready` | Load flight; check log + `/api/telemetry` |
| Mesh **white** sphere | Old web bundle or map not bound | Hard refresh `?v=107`; see lab |
| Mesh **black** sphere | Old bundle (map-only material bug) | Same — use v107+ |
| `bodyTextureStatus: pending` | Export still running | Wait ~5 s after flight load |
| `failed` / `unsupported` | Exotic shader / no `_MainTex` | Color fallback; check KSP log for body name |
| JPEG 404 | No flight export yet | Load flight first |
| Stale texture after mod | Fingerprint / revision | Reload flight; URL `?rev=` should change |
| Kerbin square icon | Wrong LOD branch | Should be round dot — report bug |
| Mun has “planet” mesh | Should not happen | Excluded by hierarchy — report bug |

---

## 11. Developer reference (files)

### 11.1 Planner + policy (`map-v3`)

| File | Purpose |
|------|---------|
| `elements/planetBody/buildPlanetBodySegments.ts` | One segment per planet |
| `elements/planetBody/planetBodyLod.ts` | Mesh ↔ icon policy |
| `elements/planetBody/planetBodyTextureFields.ts` | Telemetry texture helpers |
| `layerFlags.ts` | `MAP_V3_LAYERS_PHASE3` |
| `MapComposer.ts` | Layer list for phase 3 |

### 11.2 Presentation (`scene/v3`)

| File | Purpose |
|------|---------|
| `layers/PlanetBodyLayer.tsx` | Visibility + segment loop |
| `layers/PlanetBodyMesh.tsx` | LOD router |
| `layers/PlanetBodyDot.tsx` | Icon LOD |
| `layers/TexturedPlanetBody.tsx` | Textured mesh |
| `layers/FlatPlanetBody.tsx` | Color fallback mesh |
| `layers/usePlanetBodyDrawMode.ts` | Per-frame LOD from camera |

### 11.3 Assets + dev

| File | Purpose |
|------|---------|
| `assets/planetBodyTextures.ts` | Loader + cache |
| `dev/PlanetTextureLab.tsx` | Lab UI |
| `dev/planet-texture-lab-entry.tsx` | Lab bundle entry |

### 11.4 Plugin (C#)

| File | Purpose |
|------|---------|
| `Textures/ScaledBodyTextureExporter.cs` | JPEG export |
| `Services/BodyTextureExportService.cs` | Flight-load queue |
| `Textures/HeliocentricPlanetFilter.cs` | Sun-child filter |
| `Telemetry/CelestialBodySnapshot.cs` | `bodyTexture*` fields |
| `Services/LocalHttpServerService.cs` | Serves `/assets/bodies/` |

### 11.5 Pattern for Phase 4–5 (moons)

Copy the same **split**:

1. Element builder under `map-v3/elements/moonBody/` (planner only).
2. LOD policy file (can share `planetBodyLod` math later).
3. Layer under `scene/v3/layers/MoonBodyLayer.tsx`.
4. Textures: only if moons get export rules in DLL (not Phase 3).

---

## 12. Related specs and revision history

| Rev | Date | Change |
|-----|------|--------|
| 1.0 | 2026-05-22 | Phase 3 operator + developer guide (bodies 3.1, textures 3.3, lab, v107) |

Formal IDs remain in [`MAP_V3_PLANET_BODY_SPEC.md`](MAP_V3_PLANET_BODY_SPEC.md) and [`MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](MAP_V3_PLANET_BODY_TEXTURE_SPEC.md).
