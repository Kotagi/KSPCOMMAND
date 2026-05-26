# Orbit offset calibration

`RootRelativePositionResolver` calibrates at capture time by comparing live `body.position - root.position` against propagated offsets at `UT = now + 60 s` for Kerbin, Mun, Moho, Eve, and Duna.

**Display authority (Phase 11):** `bodies[].positionRootRelativeMeters` and all body-orbit trail samples use `GetBodyDisplayRootRelative` → `GetBodyRootRelativeForTrailSample`.

**Heliocentric planets (parent = Sun):** always `getRelativePositionAtUT` — **not** governed by flip/no-flip calibration. See [`HELIOCENTRIC_ORBIT_FRAME.md`](HELIOCENTRIC_ORBIT_FRAME.md).

**Moons:** flip/no-flip mode governs parent-chain relative offsets and diagnostic flip propagation only.

**Moon orbit trail rings (resolver `"5"`):** All 128 capture samples use propagated parent chain (`orbitTrailRingSample: true`) — no live@sample0 on the polyline. Icons at `T_now` may still use live. Web: [`MAP_V3_MOON_ORBIT_SPEC.md`](MAP_V3_MOON_ORBIT_SPEC.md) § Standard procedure.

## Modes

| Mode | Behavior |
|------|----------|
| `flipRelative` | Parent chain + `getRelativePositionAtUT` with axis flip (default when it wins calibration) |
| `noFlipRelative` | Same chain without flip |

The winning mode is exported in `frameDiagnostics.orbitOffsetMode` on every telemetry snapshot (schema v8).

## When to re-check

- After KSP or mod updates that change orbit API behavior
- If `liveToSample0Meters` exceeds 1 m with a fresh DLL load
- If `verify-telemetry.ps1` fails with propagation residuals but analytic looks correct

Flight sign-off: record the mode from HUD truth banner or `GET /api/diagnostics`.
