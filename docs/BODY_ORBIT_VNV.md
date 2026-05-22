# Body–Orbit Alignment — Verification and Validation

This document supports body icon / orbit trail alignment and Phase 11 frame truth (schema v8, resolver v2).

## Acceptance criteria

| ID | Criterion | Pass condition |
|----|-----------|----------------|
| AC-001 | Kerbin icon on grey trail | Visual + `liveToAnalyticMeters < 5e4` |
| AC-002 | Moho, Eve, Duna on trails | Same |
| AC-003 | Mun around Kerbin | Residual &lt; 50 km; concentric visually |
| AC-004 | Coplanar heliocentric set | `planeAngleToAnalyticDegrees < 0.1` |
| AC-005 | Period closure | `periodClosureMeters < 50 km` (Kerbin in Sun frame; moons in parent frame) |
| AC-006 | No stray trails | `trailRenderMode=hidden` when validation fails |
| AC-007 | HUD per-body table | **Show body orbit QA** lists all paths |
| AC-008 | Trail alignment script | `scripts/verify-telemetry.ps1` exits 0 |
| AC-009 | Icon uses trail authority | `liveToSample0Meters ≤ 1 m`; `iconTrailSample0ResidualMeters ≤ 1 m` |
| AC-010 | Live vs KSP true API | `liveVsTrueDeltaMeters ≤ 1 km` for Sun-children at now (or documented waiver) |
| AC-011 | Same-UT validation honest | `ephemerisValidationResidualMeters ≤ 1 Mm`; T+60s not primary HUD alarm |
| AC-012 | Position script | `scripts/verify-body-positions.ps1` exits 0 |

## Metric glossary (Phase 11)

| Metric | Meaning |
|--------|---------|
| `positionRootRelativeMeters` | **Display authority** — `GetBodyDisplayRootRelative` (trail/true resolver) |
| `positionLiveRootRelativeMeters` | `body.position - root.position` at capture |
| `positionTrueRootRelativeMeters` | `getTruePositionAtUT` world difference into root frame |
| `liveVsTrueDeltaMeters` | ‖live − true‖ at `T_now` |
| `iconTrailSample0ResidualMeters` | max ‖icon − trail[0]‖ (same-UT) |
| `ephemerisValidationResidualMeters` | max same-UT: icon↔trail₀, trail self-consistency |
| `ephemerisLivePropagationResidualMeters` | **Diagnostic:** max ‖pos(T) − pos(T+60s)‖ (orbital motion, not error) |
| `bodyOrbitFlipPropagationResidualMeters` | **Diagnostic:** trail sample vs flip `GetBodyRootRelative` |
| `maxSampleToTrailFrameMeters` | trail sample vs trail-frame recompute (was `maxSampleToTrueMeters`) |

## Flight QA checklist (operator)

1. Quit KSP → run `scripts/build.ps1` → `scripts/install.ps1` (see [INSTALL_DLL.md](INSTALL_DLL.md)) → restart KSP.
2. Open `http://127.0.0.1:8750/?v=85` (hard refresh; match `index.html` and `KSP_WEB_MAP_UI_VERSION`).
3. Optional sample density: in telemetry JSON, `bodyOrbitPaths[].samples.length` should be **128** per planet (DLL `BodyOrbitPathSampleCount`). Tuning: [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md) §12.
4. Stable Kerbin orbit: run `scripts/verify-telemetry.ps1` and `scripts/verify-body-positions.ps1` — expect **VERIFY PASS**.
5. **Full system** camera: Sun at origin, Kerbin on grey ring, icon on ring.
6. Open HUD → **Show body orbit QA** — confirm `live↔s0` ≈ 0 m; primary banner shows same-UT validation (not Gm-scale).
7. Optional: `node scripts/diagnose-planet-positions.mjs` — clock table vs KSP neutral map.
8. Kerbin escape / high eccentricity: trails **hidden** or **samples**; use patch conic checks (not Kerbin trail angle from `verify-vessel-frame`).
9. If warnings appear in `Player.log` (`[KspWebMap]`), attach excerpt with snapshot id.

## Automated tests

| Layer | Command / location |
|-------|-------------------|
| Unit (web) | `cd web && npm test` |
| Golden | `web/fixtures/telemetry-kerbin-stable.json` |
| Flight trails | `scripts/verify-telemetry.ps1` |
| Flight positions | `scripts/verify-body-positions.ps1` |

## Diagnostics endpoints

- `GET /api/telemetry` — full snapshot (`positionValidation`, per-body live/true fields)
- `GET /api/diagnostics` — `positionValidation`, validation residuals, per-path `validation`

## Thresholds (reference)

| Metric | Display / analytic gate | Log / script fail |
|--------|-------------------------|-------------------|
| `liveToSample0Meters` | ≤ 1 m | &gt; 1 m |
| `iconTrailSample0ResidualMeters` | ≤ 1 m | &gt; 1 m |
| `liveVsTrueDeltaMeters` (Sun-children) | ≤ 1 km | &gt; 1 km |
| `ephemerisValidationResidualMeters` | ≤ 1 Mm | &gt; 1 Mm |
| `liveToAnalyticMeters` | ≤ 50 km for analytic mode | &gt; 1 Mm with visible trail |
| `periodClosureMeters` | ≤ 50 km | &gt; 50 km → hidden |
| `maxSampleToRecomputedMeters` | ≤ 1 Mm for samples mode | &gt; 1 Mm → hidden |

## Regression vs KSP map

1. In KSP tracking map, reset rotation to a **neutral** full-system view (or note current rotation).
2. Open web **Full system** view; compare Moho, Eve, Kerbin, Duna, Jool **relative spacing** — icons should sit on grey trails.
3. Compare **clock** on ecliptic XZ (`eclipticLongitudeDegrees` or `diagnose-planet-positions.mjs`): ~15° tolerance is acceptable if spacing matches and only map rotation differs.
4. Do not treat `ephemerisLivePropagationResidualMeters` (60s separation) as an icon position bug.
