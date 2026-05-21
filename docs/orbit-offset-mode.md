# Orbit offset calibration

`RootRelativePositionResolver` calibrates at capture time by comparing live `body.position - root.position` against propagated offsets at `UT = now + 60 s` for Kerbin, Mun, Moho, Eve, and Duna.

**Display authority (Phase 11):** `bodies[].positionRootRelativeMeters` and trail sample 0 use `GetBodyDisplayRootRelative` (trail/true path). Flip/no-flip mode still governs relative offsets in parent chains and diagnostic flip propagation only.

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
