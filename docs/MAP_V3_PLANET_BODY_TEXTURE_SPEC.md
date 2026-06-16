# Map V3 — Planet body textures (plugin export)

| Field | Value |
|-------|-------|
| **Document ID** | MAP-V3-PLANET-TEXTURE-001 |
| **Revision** | 1.2 (2026-05-23) |
| **Phase** | 3.3 (`planetBody` mesh LOD) |
| **Scope** | Heliocentric **planet** ScaledSpace albedo → HTTP JPEG → WebGL mesh LOD |
| **UI build** | `112-planet-texture-flipy` (`?v=112`) |
| **Depends on** | [`MAP_V3_PLANET_BODY_SPEC.md`](MAP_V3_PLANET_BODY_SPEC.md) (LOD, position, icon dots) |

## References

| Source | Role |
|--------|------|
| SCANsat | In-game `scaledBody` material read → `Graphics.Blit` / `ReadPixels` → cached map texture |
| [`MAP_V3_PLANET_BODY_SPEC.md`](MAP_V3_PLANET_BODY_SPEC.md) | Sister element — icon LOD, frame authority |
| [`MAP_V3_PLANET_ORBIT_SPEC.md`](MAP_V3_PLANET_ORBIT_SPEC.md) | Planet inclusion (`orbit.referenceBody === root`) |
| Telemachus / kerbalmaps tiles | **Not used** — 2D tiles, not ScaledSpace |
| Manual `kerbin00.png` PoC | **Deprecated** — replaced by plugin export |
| [`MAP_V3_PHASE3_GUIDE.md`](MAP_V3_PHASE3_GUIDE.md) | Operator + developer how-to (export, load, lab, troubleshooting) |

## Purpose

**Objective:** When Map V3 draws a planet in **mesh LOD**, the sphere shall display the same ScaledSpace albedo the player sees in the KSP tracking map, without shipping Squad texture binaries in the public git repository.

**Non-goals:** Sun texture (Phase 1 `starMarker`), SOI, labels, `meshStandardMaterial` lighting model, redistributing stock assets.

**Phase 4.1 (moons):** Stock moons of heliocentric planets use the same ScaledSpace export path and web `PlanetBodyMesh` / `TexturedPlanetBody` stack when the host planet is in mesh LOD (`MoonBodyLayer`). See [`MAP_V3_MOON_ORBIT_SPEC.md`](MAP_V3_MOON_ORBIT_SPEC.md).

## Inclusion rules

| Body class | Export | Telemetry `bodyTexture*` | Web mesh texture |
|------------|--------|--------------------------|------------------|
| Heliocentric planet (`orbit.referenceBody === root`, not root) | Yes | Yes | Yes when `ready` |
| Root / Sun | No | Omitted | No |
| Stock moon (parent heliocentric planet) | Yes | Yes | Yes when host planet mesh LOD + `ready` |
| Other | No | Omitted | No |

Same planet set as [`MAP_V3_PLANET_BODY_SPEC.md`](MAP_V3_PLANET_BODY_SPEC.md) (`hierarchy.planetNames`).

## Requirements matrix

### Functional

| ID | Requirement |
|----|-------------|
| **P3T-FR-01** | On first valid flight scene after load, the plugin **shall** queue export for each heliocentric planet. |
| **P3T-FR-02** | Export source **shall** be `CelestialBody.scaledBody` `MeshRenderer.sharedMaterial` per §4. |
| **P3T-FR-03** | Output **shall** be `{WebRoot}/assets/bodies/{sanitizedName}.jpg`. |
| **P3T-FR-04** | Telemetry **shall** publish `bodyTextureUrl`, `bodyTextureRevision`, `bodyTextureStatus` on planet `bodies[]` entries. |
| **P3T-FR-05** | Web mesh LOD **shall** load `bodyTextureUrl` when `bodyTextureStatus === "ready"`; else orbit-color `meshBasicMaterial`. |
| **P3T-FR-06** | Icon LOD **shall not** fetch body textures ([`PlanetBodyDot.tsx`](../web/src/scene/v3/layers/PlanetBodyDot.tsx)). |
| **P3T-FR-07** | Re-export **shall** occur only when material fingerprint changes or JPEG missing; **not** each telemetry tick. |
| **P3T-FR-08** | Kerbin-only PoC (`kerbin00.png`, `KerbinTexturedBody`) **shall** be removed after generic path ships. |

### Non-functional

| ID | Requirement |
|----|-------------|
| **P3T-NFR-01** | Max dimension 1024 px; JPEG quality 85. |
| **P3T-NFR-02** | Total export ≤ 5 s stock system (log per-body ms; warn if exceeded). |
| **P3T-NFR-03** | Target ≤ 400 KB per JPEG at 1024 (warn if larger). |
| **P3T-NFR-04** | Export **shall** yield one body per frame (main-thread coroutine). |
| **P3T-NFR-05** | Web cache keyed by `bodyTextureRevision`; reload on revision change only. |
| **P3T-NFR-06** | `GET /assets/bodies/*` **shall** return `Cache-Control: public, max-age=31536000`. |

### Security / legal

| ID | Requirement |
|----|-------------|
| **P3T-SEC-01** | Filename stem: `[A-Za-z0-9_-]` only. |
| **P3T-SEC-02** | HTTP static serve **shall** keep `IsPathInsideWebRoot` guard. |
| **P3T-SEC-03** | Exported JPEGs **gitignored** under `GameData/KspWebMap/Web/assets/bodies/`. |
| **P3T-SEC-04** | Repo ships README + `.gitkeep` only (no Squad pixels). |

## KSP material model (§4)

### Shader property resolution (priority)

| Step | Property | When |
|------|----------|------|
| 1 | `_MainTex` | `HasProperty("_MainTex")` and non-null |
| 2 | `_ColorMap` | Parallax / no `_MainTex` |
| 3 | `_DetailCloudPatternTexture` | Gas giants — composited over main via Blit when both exist |

### Support tiers

| Tier | Shaders | `bodyTextureStatus` |
|------|---------|---------------------|
| A | Stock KSP Scaled (rocky + gas) | `ready` |
| B | Mods using same property names | `ready` |
| C | No resolvable albedo | `unsupported` |
| D | Blit/read failure | `failed` (one retry per flight) |

Fingerprint for invalidation: `shader.name` + texture instance IDs for properties in the table above. Stored in `{name}.jpg.meta` next to JPEG.

## Export pipeline (§5)

1. `BodyTextureExportService` starts on flight load; builds heliocentric planet list.
2. Per body (one per frame): read fingerprint; if JPEG + `.meta` match → `ready` with file-hash revision.
3. Else `ScaledBodyTextureExporter.TryExport` → Blit to `RenderTexture` → `ReadPixels` → resize → `EncodeToJPG(85)` → write files.
4. Revision = first 8 hex chars of SHA-256(file bytes).
5. `TelemetrySnapshotService` copies registry state into `CelestialBodySnapshot` (read-only; no export in 0.2 s capture loop).

## Telemetry & HTTP contract (§6)

**Schema:** bump `schemaVersion` to **9** (optional fields backward-tolerant on web).

Per planet body:

```json
{
  "name": "Kerbin",
  "bodyTextureUrl": "/assets/bodies/Kerbin.jpg?rev=a1b2c3d4",
  "bodyTextureRevision": "a1b2c3d4",
  "bodyTextureStatus": "ready"
}
```

| `bodyTextureStatus` | Meaning |
|---------------------|---------|
| `pending` | Queued or exporting |
| `ready` | JPEG on disk; URL valid |
| `failed` | Export error; web uses color fallback |
| `unsupported` | No albedo property; color fallback |

**HTTP:** `LocalHttpServerService` serves `/assets/bodies/{file}` with long-lived cache header.

## Web consumer (§7)

| File | Role |
|------|------|
| `web/src/assets/planetBodyTextures.ts` | `loadBodyTexture`, revision cache; **`flipY = true`**; **`BODY_TEXTURE_MIRROR_U = false`** (longitude flip-X in DLL export) |
| `web/src/scene/v3/layers/TexturedPlanetBody.tsx` | `MeshBasicMaterial` + `<primitive attach="material" />` |
| `web/src/scene/v3/layers/PlanetBodyMesh.tsx` | LOD router; mesh under `PlanetBodyOrientedGroup` → `PlanetBodyMeshPoleFrame` |

**Known limitation:** Flat JPEG on generic `SphereGeometry` may not match KSP tracking-map **longitude** exactly (meridian offset). North/south and spin/obliquity are separate concerns — see [`MAP_V3_PHASE3_GUIDE.md`](MAP_V3_PHASE3_GUIDE.md) §13.

## Performance budgets (§8)

| Metric | Budget |
|--------|--------|
| Export wall time (stock) | ≤ 5 s |
| Per-body frame slice | 1 body / frame |
| JPEG max edge | 1024 px |
| Telemetry enrichment | O(1) registry lookup per body |

## Packaging (§9)

- Export directory: `GameData/KspWebMap/Web/assets/bodies/` (created at runtime).
- Build: `scripts/build.ps1` ensures directory exists in package staging.
- Manual copy from `assets/planets/` documented as legacy in [`assets/planets/README.md`](../assets/planets/README.md).

## Verification (§10)

Operator: flight load → `http://127.0.0.1:8750/?v=112` → **3D Map V3** → zoom to mesh LOD.

| ID | Criterion |
|----|-----------|
| P3T-01 | Kerbin mesh shows continents (not flat map color) |
| P3T-02 | Duna, Eve, Jool recognizable |
| P3T-03 | Icon LOD: orbit-colored dots only |
| P3T-04 | Telemetry planets have `bodyTextureStatus`; `ready` has URL |
| P3T-05 | `GET /assets/bodies/Kerbin.jpg` → 200 |
| P3T-06 | Mod texture change → revision change → web reload |
| P3T-07 | P3-01–P3-12 regression |
| P3T-08 | No console errors; failed → color fallback |
| P3T-09 | `npm test` + `npm run build` |

See [`MAP_V3_ACCEPTANCE.md`](MAP_V3_ACCEPTANCE.md) § Phase 3.3.

## Developer verification — planet texture lab (§10a)

The lab is **not** bundled into `ksp-solar-map.js`. It ships as a separate entry (`planet-texture-lab.js`) and HTML page.

| Item | Path |
|------|------|
| Lab UI | [`web/src/dev/PlanetTextureLab.tsx`](../web/src/dev/PlanetTextureLab.tsx) |
| Lab entry | [`web/src/dev/planet-texture-lab-entry.tsx`](../web/src/dev/planet-texture-lab-entry.tsx) |
| Dev HTML (Vite) | [`web/dev/planet-texture-lab.html`](../web/dev/planet-texture-lab.html) |
| Packaged HTML | `GameData/KspWebMap/Web/planet-texture-lab.html` |
| How-to | [`web/dev/README.md`](../web/dev/README.md) |

Use after export or material-loader changes, before full-map manual QA.

## Phased delivery (§11)

| Phase | Deliverable |
|-------|-------------|
| 3.3.2 | This spec + cross-links + `bodies/` README |
| 3.3.3 | C# exporter + export service |
| 3.3.4 | Telemetry + web consumer + UI `107` (material ref fix) |
| 3.3.5 | `verify-telemetry.ps1` texture checks + program state |

## Revision history

| Rev | Date | Change |
|-----|------|--------|
| 1.0 | 2026-05-22 | Initial Option 1 plugin export program |
| 1.1 | 2026-05-22 | [`MAP_V3_PHASE3_GUIDE.md`](MAP_V3_PHASE3_GUIDE.md) hub |
| 1.2 | 2026-05-23 | UI v112; `flipY = true`; meridian offset note; oriented mesh stack |
