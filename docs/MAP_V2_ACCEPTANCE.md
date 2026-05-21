# Map V2 — Acceptance thresholds

Compare **in-game KSP map** vs web **View → 3D Map V2** on the same save and UT. Ctrl+F5 after install.

## Per-phase checklist

| Phase | Pass criteria |
|-------|----------------|
| 0 | `3d-v2` loads dark scene, no console errors; `3d` v1 unchanged |
| 1 | Star centered after Recenter; matches KSP solar view |
| 2 | All planets at solar zoom show colored closed trails; mod bodies with `bodyOrbitPaths` appear |
| 3 | Planet dots on trails; residual ≤ **50 km** analytic (`ANALYTIC_DISPLAY_THRESHOLD_METERS`) or sample self-consistency per telemetry |
| 4 | Mun/Minmus rings centered on Kerbin; inclination like KSP Kerbin system map |
| 5 | Moons on Phase 4 trails |
| 6 | Vessel icon matches KSP ship icon (solar + Kerbin views) |
| 7 | Solar zoom: large bodies → icon dots (Jool); zoom in: mesh sphere (Kerbin) |
| 8 | Cyan leg from ship to encounter only; max residual **1 km** vs vessel position on leg |
| 9–11 | Labels / yellow future route / SOI when zoomed |
| 12–13 | Camera Recenter, selection (shared with v1 store) |
| 14 | All reference screenshots signed off |

## Automated (Vitest)

- `buildBodyOrbitTrail.test.ts`, `buildPatchConic.test.ts` — shared math
- Fixture: `web/fixtures/telemetry-kerbin-stable.json`
- Scripts: `scripts/verify-body-positions.mjs`, `scripts/verify-vessel-frame.mjs`

## Residual HUD (v2)

Read-only worst `bodyOrbitAnalyticResidualMeters` across `bodyOrbitPaths` in `MapHudV2`.
