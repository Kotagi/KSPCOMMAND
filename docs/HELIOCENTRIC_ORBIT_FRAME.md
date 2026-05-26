# Heliocentric orbit frame — inclination / plane alignment

**Document ID:** MAP-HELIO-FRAME-001  
**Revision:** 1.0 (2026-05-22)  
**Applies to:** Map **V3** planet orbits (Phase 2), DLL `RootRelativePositionResolver`  
**Not a product version:** This is **not** “Map V4.” See [Version names](#version-names-do-not-confuse) below.

---

## Summary

Stock KSP map planet rings use the same orbital frame as `orbit.inclination` and `orbit.LAN` (parent-relative Kepler elements). Our web map draws **telemetry sample polylines** from the DLL. A **mixed capture frame** made rings look **~180° wrong on inclination** while planet icons and prograde/retrograde styling still looked plausible at the current universal time.

**Fix (2026-05-22):** For bodies whose `referenceBody` is the solar root (Sun), **every** trail sample and display position uses `orbit.getRelativePositionAtUT(UT)` — including sample `0` at capture UT. The live shortcut (`body.position - root.position`) runs only for **moons** and other non-heliocentric paths.

---

## Version names (do not confuse)

| Label | Meaning | Current example |
|-------|---------|-----------------|
| **Map V3** | 3D modular solar map program / phase | Planet orbits shipped in phase 2 |
| **`KSP_WEB_MAP_UI_VERSION`** | Web bundle cache-bust tag | `94-heliocentric-relative-unified` |
| **`?v=` in `index.html`** | Must match web deploy | `94` |
| **`frameDiagnostics.resolverVersion`** | DLL **frame-authority** revision (telemetry only) | `"4"` — not a map product version |

When debugging, say: *“Map V3, UI 94, resolver frame revision 4.”*

---

## Symptoms (how to recognize this class of bug)

| Observation | Typical cause |
|-------------|----------------|
| Planet **on** the trail at one point; **inclination / tilt** vs KSP map wrong on Moho, Eve, Jool | Mixed frame on the sample polyline, or wrong API vs elements |
| Rings look **mirrored** or **~180° off** vs stock map; clock/spacing on ecliptic still roughly OK | Plane normal opposite to element plane |
| HUD: `live↔s0 ≈ 0`, `icon↔trail0 ≈ 0`, but `live↔ana` **Mm–Gm** | Samples match icons; analytic element ring disagrees (frame mismatch) |
| `KSP.log`: `[KspWebMap] Body orbit Moho: liveToAnalytic=…` **1e9+ m** every ~10 s | Same — validation sees element vs sample plane gap |
| `planeAngleToAnalyticDegrees` **null** (older DLL) | Broken normal in `PerifocalToInertial((0,0,1))` — fixed via `OrbitNormalKspLocal` |
| After a partial fix: trails **worse**, planets **clustered** or dots off rings | Sample `0` still **live**, samples `1+` **relative** (still mixed) |

**Not usually this bug:** wrong **phase** along the ring (planet at 3 o’clock vs 9 o’clock with same plane) — check clock/UT anchor (§7 in [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md)).

---

## Root cause (2026-05-22 incident)

### Intended behavior

- DLL captures **128** positions per orbit period: `sampleUt = T_capture + (i/N)*period`, `i = 0…N-1`.
- Web **densifies** to **512** vertices and draws a closed ring (samples-first).
- Icons use `GetBodyDisplayRootRelative` → same resolver as trail samples.

### What went wrong (two failure modes)

**A — Mixed frame on the ring (primary, shipped pre-fix)**

| Sample index | UT | Old resolver behavior |
|--------------|-----|------------------------|
| `0` | `T_capture` | **Live** shortcut: `body.position - Sun.position` (within 1 s tolerance) |
| `1…127` | `T_capture + Δt` | **`getTruePositionAtUT` − Sun true** (heliocentric “true” world) |

The polyline was **glued** from two different KSP position APIs. At `T_now` live and true are often close (~0 m `liveVsTrue`), so the icon sat on sample `0`, but the rest of the ring lay in a **different orbital plane** (~180° normal flip vs `orbit.inclination`). Visually: “inclination 180° off”; prograde tail could still look fine locally.

**B — Incomplete fix (regression, internal builds only)**

Switching samples `1+` to `getRelativePositionAtUT` **without** moving the live shortcut **after** the heliocentric branch left sample `0` on live and the rest on relative — still a kink / wrong ring.

**C — Correct fix**

For `parent == rootBody` (Sun):

1. Handle **before** the live shortcut.
2. Always `return body.orbit.getRelativePositionAtUT(sampleUniversalTime)`.
3. Same path for icons (`GetBodyDisplayRootRelative` → `GetBodyRootRelativeForTrailSample`).

Moons (`parent != Sun`) keep: live at `T_now`, parent-chain propagation + calibrated `flipRelative` / `noFlipRelative` ([`orbit-offset-mode.md`](orbit-offset-mode.md)).

### What we do **not** use for display trails

| API | Role |
|-----|------|
| `getTruePositionAtUT` − root true | **Diagnostic only** (`positionTrueRootRelativeMeters`, `TryGetTrueRootRelative`) — do not draw planet trails from this |
| Manual Y-flip / inclination offset in web | **Bandaid** — rejected |
| Analytic Kepler ring when ≥2 samples exist | Wrong for icons when samples disagree; samples-first policy unchanged |

---

## Planet orientation (Phase 3.4)

`body.rotation` and `body.angularVelocity` are captured in **Unity world** space, but heliocentric **positions** use `getRelativePositionAtUT` (root-relative / parent-orbit axes). Those differ by a fixed **+90° about +X** mapping (`OrbitFrameMapping.WorldRotationToRootRelativeFrame`).

Without that transform, spin axes look **in-plane** (poles toward the Sun) while orbits stay in the ecliptic.

| File | Responsibility |
|------|----------------|
| `OrbitFrameMapping.cs` | `WorldVectorToRootRelativeFrame`, `WorldRotationToRootRelativeFrame` |
| `BodyOrientationResolver.cs` | Applies world → root-relative before telemetry JSON |

---

## Code authority (single place to change)

| File | Responsibility |
|------|----------------|
| `src/KspWebMap/Telemetry/RootRelativePositionResolver.cs` | `GetBodyRootRelativeForTrailSample` — Sun-child branch **before** live shortcut |
| `src/KspWebMap/Telemetry/TelemetrySnapshotService.cs` | `CaptureBodyOrbitPaths`, `CaptureBodyTrailSamplePosition` |
| `src/KspWebMap/Telemetry/BodyOrbitDiagnostics.cs` | `planeAngleToAnalyticDegrees`, `liveToAnalyticMeters` |
| `src/KspWebMap/Telemetry/OrbitFrameMapping.cs` | `OrbitNormalKspLocal` for plane QA |
| `web/.../densifyPlanetOrbitTrail.ts` | Samples-first (unchanged); draws what DLL sends |

---

## Diagnostics (flight)

### HUD — Show body orbit QA

Per planet, expect when frame is correct:

| Metric | Good |
|--------|------|
| `live↔s0` | ≈ 0 m |
| `live↔ana` | **km** scale (analytic ring near sample plane), not Gm |
| `plane` | ≈ **0.00°** (coplanar with elements) |
| `mode` | `samples` on typical saves |

### Scripts

```bash
node scripts/diagnose-planet-positions.mjs http://127.0.0.1:8750
```

Includes `live↔ana`, `plane`, `mode` per planet.

### `KSP.log`

Throttled warnings when `liveToAnalyticMeters > 1e6 m`. After fix, heliocentric planets should **stop** spamming Gm-scale warnings.

### `frameDiagnostics` (telemetry JSON)

- `resolverVersion`: `"4"` — frame revision with unified Sun-child relative propagation.
- `orbitOffsetMode`: still `flipRelative` or `noFlipRelative` for **moon chains** only.

---

## Pass criteria (regression)

Align with [`BODY_ORBIT_VNV.md`](BODY_ORBIT_VNV.md) AC-004, AC-001/002:

1. Visual: **Full system** — mutual tilt Moho / Eve / Jool matches KSP tracking map (same camera pitch helps).
2. `planeAngleToAnalyticDegrees < 0.1` for coplanar heliocentric set.
3. `liveToAnalyticMeters < 50 km` when analytic mode would be allowed (often still `samples` mode).
4. `liveToSample0Meters ≤ 1 m`, `maxSampleToRecomputedMeters` small.

---

## If this regresses again — checklist

1. Confirm DLL loaded: `frameDiagnostics.resolverVersion` in `/api/telemetry` (expect `"4"` or later frame revision, not old `"2"` without plane fix).
2. Confirm web cache: console `UI 94-heliocentric-relative-unified`, `?v=94` hard refresh.
3. In `GetBodyRootRelativeForTrailSample`, verify Sun-child branch is **above** live shortcut and uses **only** `getRelativePositionAtUT`.
4. Compare sample plane vs elements: `plane` in QA; optional manual check in `diagnose-planet-positions.mjs`.
5. Do **not** “fix” by negating inclination in Three.js or forcing analytic rings while samples exist.
6. Read §12 in [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) if the issue is faceting or icon offset with **small** `live↔ana` (vertex count / samples-first, not frame).

---

## Body orientation (Phase 3.4)

Planet **mesh** attitude uses the same root-relative axis convention as trail positions:

| Quantity | Mapping |
|----------|---------|
| `body.rotation` | `OrbitFrameMapping.WorldRotationToRootRelativeFrame` before telemetry |
| `body.angularVelocity` | `OrbitFrameMapping.WorldVectorToRootRelativeFrame` |

Web: [`kspBodyOrientation.ts`](../web/src/coords/kspBodyOrientation.ts) + [`MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md`](MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md).

**Do not** compare raw Unity `body.rotation` to root-relative positions without this mapping.

---

## Related docs

- [`ARCHITECTURE.md`](ARCHITECTURE.md) — Shared solar-system frame (updated)
- [`MAP_V3_PLANET_ORBIT_SPEC.md`](MAP_V3_PLANET_ORBIT_SPEC.md) — Planet pipeline
- [`MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md`](MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md) — Planet mesh tilt/spin
- [`MAP_V3_PHASE3_GUIDE.md`](MAP_V3_PHASE3_GUIDE.md) §13 — Phase 3 lessons learned
- [`BODY_ORBIT_VNV.md`](BODY_ORBIT_VNV.md) — Acceptance + QA checklist
- [`orbit-offset-mode.md`](orbit-offset-mode.md) — Moon flip calibration (not Sun children)
- [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) §13 — Short pointer + symptom table

---

## Changelog

| Date | Change |
|------|--------|
| 2026-05-22 | Unified Sun-child `getRelativePositionAtUT`; `OrbitNormalKspLocal`; HUD `plane` metric; UI `94-heliocentric-relative-unified`; `resolverVersion` `"4"` |
| 2026-05-23 | Documented orientation capture uses same world→root-relative mapping as positions |
