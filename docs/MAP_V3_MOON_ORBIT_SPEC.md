# Map V3 — Moon orbit paths (element specification)

| Field | Value |
|-------|-------|
| **Document ID** | MAP-V3-MOON-ORBIT-001 |
| **Revision** | 1.3 (2026-05-26) |
| **Phase** | 4 — `moonOrbit` element only |
| **Scope** | Moon orbit polylines; moon **bodies** (mesh + texture) in [`MoonBodyLayer`](../web/src/scene/v3/layers/MoonBodyLayer.tsx) — see [`MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](MAP_V3_PLANET_BODY_TEXTURE_SPEC.md) § Phase 4.1 |
| **UI build** | `124-moon-body-texture-mesh` (`?v=124`) |
| **DLL frame** | `frameDiagnostics.resolverVersion` **`"6"`** (trail + moon icon both propagated) |
| **Sister spec** | [`MAP_V3_PLANET_ORBIT_SPEC.md`](MAP_V3_PLANET_ORBIT_SPEC.md) rev 1.2 |
| **Frame guide** | [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md) § Moon trail rings |

## References

| Source | Role |
|--------|------|
| KSP in-game map | Per-moon trail colors and closed paths around parent |
| [`MAP_V3_PLANET_ORBIT_SPEC.md`](MAP_V3_PLANET_ORBIT_SPEC.md) | Sister element — **same samples-first contract** |
| [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md) | Planet + moon inclination / mixed-frame rules |
| [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) | Motion tail §2, vertex/sample tuning §12–13 |
| [`orbit-offset-mode.md`](orbit-offset-mode.md) | Moon flip/no-flip calibration (DLL) |
| [`BODY_ORBIT_VNV.md`](BODY_ORBIT_VNV.md) | AC-003 Mun, AC-005 period closure |
| [`MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md`](MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md) | Mesh attitude only — **not** moon orbit placement |

## Purpose

Render closed moon orbit polylines (Mun, Minmus, mod moons, etc.) on Map V3 with the **same frame discipline as heliocentric planet rings**: one DLL authority per ring, samples-first telemetry, 128 DLL samples → 512 web vertices, motion tail, per-body colors.

Moon trails are visible **only** when the parent planet is in **mesh** LOD (SOI-zoomed).

---

## Standard procedure (normative — do not deviate)

This is the **only** approved pipeline. It mirrors the planet orbit fix in [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md).

### 1. DLL capture (single API per ring)

| Step | Authority |
|------|-----------|
| Sample `i = 0…127` at `UT = T_capture + (i/N)×period` | `GetBodyRootRelativeForTrailSample(..., orbitTrailRingSample: true)` |
| Every sample, including `UT ≈ T_now` | Parent chain + `getRelativePositionAtUT` + calibrated `flipRelative` / `noFlipRelative` |
| **Never** on trail capture | Live shortcut `body.position − root.position` mixed with propagated samples on the same polyline |

**Moon icons** at capture UT use the **same** propagated parent chain as trail samples (`GetBodyDisplayRootRelative` → `orbitTrailRingSample: true` for `parent != Sun`). `positionLiveRootRelativeMeters` remains for QA only.

**Code:** `RootRelativePositionResolver.cs` (`ResolverVersion = "6"`), `UseTrailPropagationForDisplay`, `TelemetrySnapshotService.CaptureBodyTrailSamplePosition`.

### 2. Web geometry (samples-first)

| Priority | Condition | Source |
|----------|-----------|--------|
| 1 | `trailRenderMode === "hidden"` | Skip path |
| 2 | ≥ 2 telemetry samples | `samplePositionsParentRelativeFromPath` — `(moon − parent)` at **same UT** |
| 3 | Valid elements, &lt; 2 samples | `analyticMoonOrbitParentRelativePoints` (Kepler fallback only) |
| 4 | Else | No segment |

**Code:** `resolveMoonOrbitSource.ts` → `resolveMoonOrbitSourcePoints`.

Per sample, parent position comes from `parentPositionRootRelativeMeters` on the sample, or parent’s `bodyOrbitPaths` at the same UT. **Do not** substitute parent’s **live** position when building sample offsets (that reintroduces mixed-frame).

### 3. Web display (one transform after DLL truth)

Parent-relative offsets are already in **solar root axes** (from DLL). SOI-focused view anchors the ring to the parent **now**:

```text
solarRoot[i] = parent.position(now) + parentRelativeOffset[i]
scene[i]     = toScenePoint(solarRoot[i], frame)   // focus shift + kspRootToThree only
```

**Code:** `moonOrbitPlacement.ts` → `parentRelativeOffsetsAnchoredToParentNow`, `moonOrbitScene.ts` → `moonOrbitPointsToScene`.

### 4. Densify, anchor, and draw

- `densifyPlanetOrbitRootPoints` → **512** vertices (same as planets).
- `sampleUniversalTimes` when source is `samples` (densified to match vertices).
- `moonOrbitPointsToScene` → scene ring; `resolveMoonOrbitAnchorIndex` → `findTrailAnchorIndex` in **scene** space vs propagated moon icon (orbit guide §3).
- `MoonOrbitLayer` → `OrbitTrailV3` (not `useV3SceneTrails` on root segments — moon segments store parent-relative points until scene step).

### Architecture diagram

```text
DLL bodyOrbitPaths (128 samples, orbitTrailRingSample)
  → samplePositionsParentRelativeFromPath (moon − parent @ each UT)
  → resolveMoonOrbitSourcePoints (samples first)
  → densifyPlanetOrbitRootPoints (512)
  → buildMoonOrbitSegments (kind: moonOrbit)
  → MoonOrbitLayer (filter: parent ∈ planetsInMeshMode)
       → parentRelativeOffsetsAnchoredToParentNow
       → toScenePoint (focus only)
       → OrbitTrailV3

PlanetBodyMesh → PlanetBodyMeshLodContext → planetsInMeshMode
```

| Layer | Responsibility |
|-------|----------------|
| `resolveMoonOrbitSource.ts` | Samples-first source selection |
| `moonOrbitGeometry.ts` | Parent-relative sample extraction |
| `moonOrbitPlacement.ts` | `parent(now) + offset` — **no** extra rotations |
| `moonOrbitScene.ts` | Solar root → scene; scene-space trail anchor |
| `buildMoonOrbitSegments.ts` | `TrajectorySegment[]` |
| `MoonOrbitLayer.tsx` | Mesh gate + scene conversion |

---

## Forbidden patterns (causes ~90° / ~180° inclination bugs)

Do **not** reintroduce these — they duplicate the planet-orbit failure mode.

| Forbidden | Why |
|-----------|-----|
| **Analytic Kepler ring when ≥2 DLL samples exist** | Elements plane ≠ captured sample plane; looks like wrong inclination |
| **`bodyOrientationRootRelative` on moon trail points** | Mesh spin frame ≠ orbit `getRelativePositionAtUT` reference frame |
| **North pole + tangent / matrix “solar rotation” of parent-relative offsets** | Second-guesses DLL; flips orbital plane normal |
| **Phase twist aligning ring to `moon.position − parent.position` in solar axes** | That subtraction is not parent reference-body frame |
| **Live parent position on sample rows** (`parentLive` fallback) | Mixes `parent(now)` with `moon(t)` from propagated samples |
| **Live moon icon + propagated ring** | Icon off trail; motion tail wrong — use resolver `"6"` display path |
| **Anchor in parent-rel only** before scene | Use `resolveMoonOrbitAnchorIndex` on scene points after placement |
| **`moon_solar(t) − parent_solar(now)` without same-UT parent** | Epicycles when parent moves on heliocentric orbit |
| **Negating inclination / manual plane flip in Three.js** | Bandaid — fix capture or placement |
| **Sun-child `getRelativePositionAtUT`-only shortcut on moons** | Moons use parent chain + flip mode, not the Sun-child branch |

**Planet mesh orientation** (`PlanetBodyOrientedGroup`, `bodyOrientationRootRelative`) is **only** for textured body meshes — never for `moonOrbit` geometry.

---

## Visibility authority (normative)

**Definition:** `planetsInMeshMode` = { `P` | `P ∈ hierarchy.planetNames` and `resolvePlanetBodyDrawMode(...) === "mesh"` }.

**Show segment** when `segment.parentBody ∈ planetsInMeshMode`.

**Registry:** [`PlanetBodyMeshLodContext`](../web/src/map-v3/PlanetBodyMeshLodContext.tsx).

---

## Requirements traceability

| Req ID | Requirement | Verification |
|--------|-------------|--------------|
| MO-REQ-01 | Samples-first when ≥2 samples | `resolveMoonOrbitSource.test.ts`, flight `geometrySource: "samples"` |
| MO-REQ-02 | Motion tail per orbit guide §2 | P4-01, P4-04, manual |
| MO-REQ-03 | 128 DLL samples / 512 web vertices | P4-06 |
| MO-REQ-04 | Separate inclusion filter from heliocentric planets | P4-05, P4-12 |
| MO-REQ-05 | Hidden when parent planet is icon | P4-02, P4-03, P4-07 |
| MO-REQ-06 | Visible when parent planet is mesh | P4-01, P4-07 |
| MO-REQ-07 | All mesh parents show their moons | P4-08 |
| MO-REQ-08 | Phase 2–3 planet features unchanged | P4-09 |
| MO-REQ-10 | Single frame authority (DLL samples + parent-now anchor only) | This doc § Standard procedure; Vitest |
| MO-REQ-11 | DLL trail rings use `orbitTrailRingSample` (resolver `"5"`) | `HELIOCENTRIC_ORBIT_FRAME.md` |

---

## Inclusion rules (`shouldIncludeMoonOrbit`)

| Body class | Included when |
|------------|----------------|
| **Moon** | `isMoonBody(ctx, name)` and `referenceBody !== ctx.rootBody` |
| **Planet (heliocentric)** | Never |
| **Root / Sun** | Never |
| **Hidden validation** | `resolveTrailRenderMode(path) === "hidden"` → exclude |

---

## Diagnostics (flight)

| Check | Good |
|-------|------|
| `frameDiagnostics.resolverVersion` | `"6"` |
| `window.KspSolarMapUiVersion` | `123-moon-orbit-icon-on-trail` |
| `window.KspSolarMapMoonOrbitDebug.geometrySource` | `"samples"` (typical save) |
| Mun `liveToSample0Meters` | ≈ 0 m |
| Jool moons vs KSP map | Inclination matches (Pol, Bop, …) |

**If inclination wrong but `live↔s0 ≈ 0`:** read § Forbidden patterns — usually analytic override, orientation on trails, or stale DLL/telemetry (need new capture after resolver `"5"`).

---

## Manual V&V procedure

1. Build/install DLL + web; **restart KSP** (load resolver `"5"`).
2. Load flight; hard refresh `?v=123`; View → **3D Map V3**.
3. Full system → no moon rings (P4-02).
4. SOI-zoom Jool (mesh) → compare all moon rings to KSP tracking map inclinations (P4-01 extended).
5. Kerbin mesh → Mun/Minmus (P4-01).
6. `.\scripts\verify-telemetry.ps1` — Mun `liveToSample0 ≤ 1 m`.
7. Console: `KspSolarMapMoonOrbitDebug` — `pointCount: 512`, `geometrySource: "samples"`.

---

## Automated verification

```powershell
cd web
npm test
npm run build
```

Key tests: `resolveMoonOrbitSource.test.ts`, `moonOrbitPlacement.test.ts`, `moonOrbitScene.test.ts`, `moonOrbitGeometry.test.ts`.

---

## If this regresses — checklist

1. Confirm **`resolverVersion` `"6"`** in `/api/telemetry` after KSP restart.
2. Confirm web **`?v=123`** and console `123-moon-orbit-icon-on-trail`.
3. Confirm debug shows **`geometrySource: "samples"`** — if `"analytic"` with 128 samples in JSON, fix `resolveMoonOrbitSource` priority.
4. Search codebase for **`bodyOrientationRootRelative`** under `moonOrbit/` — must be **absent** from trail placement.
5. Search for **`parentReferenceOffsetsToSolarRoot`** / **`rotationParentReferenceToSolar`** — removed; use `moonOrbitPlacement` only.
6. Read [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md) § Moon trail rings.
7. **Do not** “fix” with inclination negation or mesh quaternion on rings.

---

## Acceptance IDs

| ID | Criterion |
|----|-----------|
| P4-01 | Parent mesh: moons closed colored rings; inclinations match KSP map |
| P4-02 | Full solar / all planets icon: no moon rings |
| P4-03 | Parent icon: moon rings off |
| P4-04 | Colors match stock table |
| P4-05 | Vitest: moons only, `referenceBody !== root`, 512 verts |
| P4-06 | Flight: ~128 samples; `verify-telemetry.ps1` PASS |
| P4-07–P4-12 | Unchanged from rev 1.0 |

---

## Revision history

| Rev | Date | Change |
|-----|------|--------|
| 1.0 | 2026-05-26 | Initial Phase 4 spec — mesh-gated visibility |
| 1.1 | 2026-05-26 | Parent-relative sample geometry (epicycle fix) |
| 1.2 | 2026-05-26 | **Samples-first + single placement** (resolver `"5"`, UI `122`); forbidden patterns; removed orientation/analytic-primary path |
| 1.3 | 2026-05-26 | **Icon on trail:** resolver `"6"` propagated moon display; scene `findTrailAnchorIndex`; UI `123` |
