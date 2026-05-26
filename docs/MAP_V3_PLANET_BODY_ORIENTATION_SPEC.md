# Phase 3 — Bodies — Tilt and Spin

| Field | Value |
|-------|-------|
| **Document ID** | MAP-V3-PLANET-ORIENTATION-001 |
| **Revision** | 1.1 (2026-05-23) |
| **Phase** | 3.4 — Bodies — Tilt and Spin |
| **Scope** | Heliocentric **planet** mesh LOD: spin-axis tilt + sidereal rotation at game UT |
| **UI build** | `112-planet-texture-flipy` (`?v=112`) |
| **Depends on** | [`MAP_V3_PLANET_BODY_SPEC.md`](MAP_V3_PLANET_BODY_SPEC.md), [`MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](MAP_V3_PLANET_BODY_TEXTURE_SPEC.md) |

## References

| Source | Role |
|--------|------|
| KSP `CelestialBody.rotation` | Stock tracking-map body orientation authority |
| kRPC `CelestialBody.rotation` / `angular_velocity` | Reference-frame pattern |
| [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md) | Same root inertial frame as positions |
| [`web/src/coords/kspBodyOrientation.ts`](../web/src/coords/kspBodyOrientation.ts) | KSP → Three basis change (lab-verified) |
| [`web/src/dev/PlanetTextureLab.tsx`](../web/src/dev/PlanetTextureLab.tsx) | Isolated POC (presets + telemetry mode) |
| [`MAP_V3_PHASE3_GUIDE.md`](MAP_V3_PHASE3_GUIDE.md) | Operator how-to |

## Purpose

**Objective:** When Map V3 draws a heliocentric planet in **mesh LOD**, the textured sphere shall match the KSP tracking map at the same universal time: correct **obliquity (spin-axis tilt)** and **rotation about that axis**, without UV scrolling or web-side frame hacks.

**Non-goals:** Sun `starMarker` rotation, moons (Phase 4–5), icon LOD rotation, 2D map `textureOffset` trick, Principia custom frames, embedding orientation in JPEG exports.

## Inclusion rules

| Body class | Orientation telemetry | Web mesh orientation | Dev spin/tilt axis |
|------------|----------------------|----------------------|---------------------|
| Heliocentric planet | Yes | Yes | Yes (mesh LOD) |
| Root / Sun | No | No | No |
| Moon | No | No | No |
| Icon LOD | N/A | **No** (flat dot) | **No** |

Same set as [`MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](MAP_V3_PLANET_BODY_TEXTURE_SPEC.md) (`hierarchy.planetNames`, `HeliocentricPlanetFilter`).

## Requirements matrix

### Functional

| ID | Requirement |
|----|-------------|
| **P3R-FR-01** | Each telemetry tick (0.2 s), DLL **shall** publish orientation fields on heliocentric planet `bodies[]`. |
| **P3R-FR-02** | Primary attitude **shall** be `bodyOrientationRootRelative` quaternion (body-fixed → `solarSystemRootCenteredInertial`). |
| **P3R-FR-03** | When `rotates === true`, DLL **shall** publish unit `spinAxisRootRelative` and `angularVelocityRootRelativeRadPerSec`. |
| **P3R-FR-04** | Mesh LOD **shall** apply attitude via [`PlanetBodyOrientedGroup`](../web/src/scene/v3/layers/PlanetBodyOrientedGroup.tsx). |
| **P3R-FR-05** | Web **shall** extrapolate spin: `q(UT) = q_delta(ω̂, ‖ω‖·(UT_now − UT_sample)) ⊗ q_sample`. |
| **P3R-FR-06** | Icon LOD **shall not** apply orientation or dev axis. |
| **P3R-FR-07** | Non-rotating or missing quaternion **shall** use identity (mesh still draws with texture/color fallback). |
| **P3R-FR-08** | Dev HUD **shall** offer **Show spin/tilt axis** — line through body center along body +Y in oriented group local frame (co-rotates with mesh). |

### Non-functional

| ID | Requirement |
|----|-------------|
| **P3R-NFR-01** | Orientation capture **shall** add ≤ 0.5 ms per heliocentric planet per tick (warn if exceeded). |
| **P3R-NFR-02** | `TelemetrySnapshot.CurrentSchemaVersion` **shall** be **10**. |
| **P3R-NFR-03** | `npm test` + `npm run build` green; [`kspBodyOrientation.test.ts`](../web/src/coords/kspBodyOrientation.test.ts) passes. |

### Frame

| ID | Rule |
|----|------|
| **P3R-FRAME-01** | Same root frame as `positionRootRelativeMeters`; **no** web-side inclination/position fixes. |
| **P3R-FRAME-02** | Three.js conversion **only** in [`kspBodyOrientation.ts`](../web/src/coords/kspBodyOrientation.ts): `Q_three = M · Q_ksp · M⁻¹`, vectors `(x, z, −y)`. |

## KSP capture model (§4)

Implemented in [`BodyOrientationResolver.cs`](../src/KspWebMap/Telemetry/BodyOrientationResolver.cs):

| Quantity | Source |
|----------|--------|
| `bodyOrientationRootRelative` | `OrbitFrameMapping.WorldRotationToRootRelativeFrame(body.rotation)` at capture UT |
| `spinAxisRootRelative` | `normalize(body.angularVelocity)` in root-relative frame; else `body.rotation * (0,1,0)` |
| `angularVelocityRootRelativeRadPerSec` | `OrbitFrameMapping.WorldVectorToRootRelativeFrame(body.angularVelocity)` |
| `rotationPeriodSeconds`, `rotationAngleRadians` | Stock fields |
| `rotates`, `inverseRotation`, `tidallyLocked` | Stock flags |

Called from `CaptureBodies` only when `HeliocentricPlanetFilter.IsHeliocentricPlanet(body, rootBody)` (mirrors texture export gate).

## Telemetry schema (§5) — version 10

Per heliocentric planet in `bodies[]`:

```json
{
  "name": "Kerbin",
  "bodyOrientationReferenceFrame": "solarSystemRootCenteredInertial",
  "bodyOrientationSampleUniversalTimeSeconds": 12345.6,
  "bodyOrientationRootRelative": { "x": 0, "y": 0, "z": 0, "w": 1 },
  "spinAxisRootRelative": { "x": 0, "y": 1, "z": 0 },
  "angularVelocityRootRelativeRadPerSec": { "x": 0, "y": 0.00029, "z": 0 },
  "rotationPeriodSeconds": 21600,
  "rotationAngleRadians": 1.23,
  "rotates": true,
  "inverseRotation": false,
  "tidallyLocked": false
}
```

Web types: [`planetBodyOrientationFields.ts`](../web/src/map-v3/elements/planetBody/planetBodyOrientationFields.ts).

## Web presentation (§6)

```text
PlanetBodyMesh (scene position)
  └── PlanetBodyOrientedGroup     ← kspRootQuaternionToThree(q at UT)
        ├── PlanetBodySpinAxisLine   (dev only)
        └── PlanetBodyMeshPoleFrame  ← kspMeshPoleOffsetQuaternion (−90° X)
              └── TexturedPlanetBody | FlatPlanetBody
```

| File | Role |
|------|------|
| [`planetBodyOrientationFields.ts`](../web/src/map-v3/elements/planetBody/planetBodyOrientationFields.ts) | Read + UT extrapolation between telemetry polls |
| [`PlanetBodyOrientedGroup.tsx`](../web/src/scene/v3/layers/PlanetBodyOrientedGroup.tsx) | Apply attitude; **production:** UT only (no `frameSpin`) |
| [`PlanetBodyMeshPoleFrame.tsx`](../web/src/scene/v3/layers/PlanetBodyMeshPoleFrame.tsx) | Align sphere texture pole with KSP body north |
| [`PlanetBodyMesh.tsx`](../web/src/scene/v3/layers/PlanetBodyMesh.tsx) | LOD router + oriented stack |
| [`kspBodyOrientation.ts`](../web/src/coords/kspBodyOrientation.ts) | KSP ↔ Three + world→root helpers |

**Texture:** Phase 3.3 JPEG remains static; **only** `PlanetBodyOrientedGroup` rotates. Longitude offset vs KSP map (UV seam) is **out of scope** for 3.4 — see [`MAP_V3_PHASE3_GUIDE.md`](MAP_V3_PHASE3_GUIDE.md) §13.

## Dev options (§7)

| Control | Store | Behavior |
|---------|-------|----------|
| **Show spin/tilt axis** | `devPlanetBodySpinAxisVisible` | Yellow line along local +Y through sphere center (mesh LOD only) |

Panel: [`PlanetBodyOrientationDevPanel.tsx`](../web/src/components/PlanetBodyOrientationDevPanel.tsx) in [`MapHud.tsx`](../web/src/components/MapHud.tsx).

## Performance (§8)

| Metric | Budget |
|--------|--------|
| Per-body capture | ≤ 0.5 ms |
| Telemetry rate | 0.2 s (unchanged) |
| Web extrapolation | O(1) quaternion math per mesh per frame |

## Verification (§9)

| ID | Criterion |
|----|-----------|
| P3R-01 | Kerbin mesh: KSC gulf at correct longitude vs KSP tracking map (same UT) |
| P3R-02 | Moho / Eve / Jool obliquity plausible vs stock map |
| P3R-03 | Icon dots unrotated |
| P3R-04 | 10× time warp: smooth surface rotation |
| P3R-05 | Dev axis passes through visible pole direction on tilted bodies |
| P3R-06 | P3-01–P3-12 and P3T-01–P3T-10 regression |
| P3R-07 | `npm test` + `npm run build` |
| P3R-08 | `scripts/verify-telemetry.ps1` orientation table |
| P3R-09 | Lab `?orientation=telemetry&body=Kerbin` loads without fallback |
| P3R-10 | HUD `v3 phase 3.4 — planet tilt and spin`; console `112-planet-texture-flipy` |

See [`MAP_V3_ACCEPTANCE.md`](MAP_V3_ACCEPTANCE.md) § Phase 3.4.

## Lab regression (§10)

[`web/dev/planet-texture-lab.html`](../web/dev/planet-texture-lab.html) — presets remain valid; telemetry mode requires schema v10 after DLL install.

## Phased delivery (§11)

| Step | Deliverable |
|------|-------------|
| 3.4.0 | This spec |
| 3.4.1 | Checkpoint commit |
| 3.4.2 | DLL + schema v10 |
| 3.4.3 | Web map + UT extrapolation |
| 3.4.4 | Dev spin/tilt axis |
| 3.4.5 | Docs, verify script, UI 108 |

## Risks (§12)

| Risk | Mitigation |
|------|------------|
| Sun world rotation | Heliocentric-only capture |
| Quaternion sign flip | Normalize on read (`w ≥ 0` optional) |
| Gas-giant cloud vs surface | Accept stock `body.rotation` (same as tracking map) |
| Lab CORS on preview | Main map same-origin `:8750` |
| Texture longitude vs KSP map | Flat albedo on generic sphere — meridian offset deferred (not spin/obliquity) |

## Revision history

| Rev | Date | Change |
|-----|------|--------|
| 1.0 | 2026-05-23 | Initial Phase 3.4 — tilt and spin (lab POC frozen) |
| 1.1 | 2026-05-23 | Root-relative capture via `OrbitFrameMapping`; pole frame; UI v112; P3R-01 meridian note |
