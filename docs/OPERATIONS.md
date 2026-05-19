# Operations

## KSP Root

This workspace targets:

```text
C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program
```

That folder should contain `KSP_x64.exe`, `GameData`, and `KSP_x64_Data`.

## Build

Run from the repository root:

```powershell
.\scripts\build.ps1 -Clean -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

Build outputs:

```text
artifacts/bin/Release/KspWebMap.dll
artifacts/package/GameData/KspWebMap
```

The build validates the required KSP and Unity assemblies before compiling:

```text
KSP_x64.exe
KSP_x64_Data/Managed/Assembly-CSharp.dll
KSP_x64_Data/Managed/UnityEngine.CoreModule.dll
KSP_x64_Data/Managed/UnityEngine.IMGUIModule.dll
```

If no .NET SDK is available, the script falls back to the Windows .NET Framework compiler.

### Web UI build (Phase 10)

When `web/package.json` exists, `build.ps1` also runs:

```powershell
cd web
npm ci   # or npm install if no package-lock.json
npm run build
```

Output is copied to `artifacts/package/GameData/KspWebMap/Web/assets/` (`ksp-solar-map.js`, `ksp-solar-map.css`).

Developers can run the solar map against a live KSP server:

```powershell
cd web
npm install
npm run dev
```

Vite proxies `/api/*` to `http://127.0.0.1:8750`.

Players only need the built assets from `build.ps1` + `install.ps1`; they do not run npm.

## Install

Install the staged package into KSP:

```powershell
.\scripts\install.ps1 -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

The installer copies from `artifacts/package/GameData/KspWebMap`. By default it cleans only the guarded destination:

```text
Kerbal Space Program/GameData/KspWebMap
```

Preview an install without writing files:

```powershell
.\scripts\install.ps1 -DryRun -Verbose -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

## Uninstall

Remove the installed mod folder:

```powershell
.\scripts\uninstall.ps1 -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

Preview an uninstall:

```powershell
.\scripts\uninstall.ps1 -DryRun -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

## Smoke Test

1. Build with `-Clean`.
2. Install the staged package.
3. Launch KSP.
4. Enter a flight scene.
5. Confirm the `KSP Web Map - Dev Test` window appears once.
6. Press `Say Hello World`.
7. Confirm the on-screen `Hello World from KSP Web Map!` message appears.
8. Leave and re-enter flight to confirm duplicate windows are not created.

## Local Server Test

The local server starts only while the flight-scene addon is active.

In flight, open:

```text
http://127.0.0.1:8750/
```

Expected result: the KSP Web Map development shell loads in the browser.

Then open:

```text
http://127.0.0.1:8750/api/health
```

Expected result:

```json
{"status":"ok","service":"KspWebMap","version":"0.1.0","serverTimeUtc":"..."}
```

## Telemetry Test

Telemetry is captured on KSP's main thread and served from plain snapshots. In flight, open:

```text
http://127.0.0.1:8750/api/telemetry
http://127.0.0.1:8750/api/active-vessel
http://127.0.0.1:8750/api/orbit
http://127.0.0.1:8750/api/bodies
```

Expected result: each endpoint returns JSON with `schemaVersion`, `snapshotId`, `capturedAtUtc`, `gameUniversalTimeSeconds`, `scene`, `valid`, and `status`.

The aggregate telemetry endpoint also includes:

```text
activeVessel
orbit
bodies
```

Field names include units where needed, such as `altitudeMeters`, `orbitalSpeedMetersPerSecond`, `apoapsisMeters`, and `periodSeconds`.

Schema version `5` includes conic, patch-route, and shared-frame map-rendering fields:

```text
orbit.classification
orbit.referenceFrame
orbit.referenceBodyRadiusMeters
orbit.sphereOfInfluenceMeters
orbit.apoapsisRadiusMeters
orbit.periapsisRadiusMeters
orbit.semiLatusRectumMeters
orbit.longitudeOfAscendingNodeDegrees
orbit.argumentOfPeriapsisDegrees
orbit.trueAnomalyDegrees
orbit.meanAnomalyRadians
orbit.epochUniversalTimeSeconds
orbit.patchStartUniversalTimeSeconds
orbit.patchEndUniversalTimeSeconds
orbit.patchStartTransition
orbit.patchEndTransition
orbit.activePatch
orbit.nextPatchReferenceBody
orbit.timeToTransitionSeconds
patchChainStatus
orbitPatches[]
orbitPatches[].patchIndex
orbitPatches[].isActivePatch
orbitPatches[].patchStartTransition
orbitPatches[].patchEndTransition
orbitPatches[].previousPatchReferenceBody
orbitPatches[].nextPatchReferenceBody
orbitPatches[].encounterBody
orbitPatches[].encounterLevel
orbitPatches[].closestEncounterUniversalTimeSeconds
orbitPatches[].captureWarning
orbitPatches[].referenceBodyPositionRootRelativeMeters
orbitPatches[].referenceBodyPositionSampleUniversalTimeSeconds
orbitPatches[].patchPlacementMode
orbitPatches[].patchPlacementWarning
activeVessel.positionReferenceFrame
activeVessel.positionRelativeToReferenceBodyMeters
activeVessel.velocityRelativeToReferenceBodyMetersPerSecond
activeVessel.rootPositionReferenceFrame
activeVessel.positionRootRelativeMeters
activeVessel.rootVelocityReferenceFrame
activeVessel.velocityRootRelativeMetersPerSecond
rootFrameName
rootBody
rootFrameOriginBody
rootFrameCapturedAtUniversalTimeSeconds
rootFrameWarning
bodies[].sphereOfInfluenceMeters
bodies[].parentBody
bodies[].positionReferenceFrame
bodies[].positionRootRelativeMeters
bodies[].velocityReferenceFrame
bodies[].velocityRootRelativeMetersPerSecond
bodies[].orbitReferenceBody
```

If no active vessel is available, telemetry should return explicit JSON with `valid: false` and a status message instead of throwing an error.

## Dashboard Test

In flight, open:

```text
http://127.0.0.1:8750/
```

Expected result: the dashboard displays live snapshot metadata, active vessel data, orbit data, body count, a body table, endpoint links, diagnostics, and raw telemetry JSON.

Normal dashboard checks:

1. Connection status should show `Connected` when telemetry is valid.
2. Snapshot ID or captured time should advance roughly once per second.
3. Active vessel should match the vessel loaded in KSP.
4. Orbit and body fields should use units such as `m`, `km`, `m/s`, `km/s`, `s`, and `deg`.
5. Browser developer console should stay free of errors during normal polling.
6. Browser network activity should not show overlapping `/api/telemetry` requests.

Degraded dashboard checks:

- Leaving the flight scene or closing KSP should show `Disconnected` while retaining the last good data if available.
- Returning to flight should recover automatically.
- If telemetry returns `valid: false`, the dashboard should show `Degraded` and the server-provided status.
- If the schema changes unexpectedly, the dashboard should show `Schema Mismatch`.

## Map Prototype Test

In flight, open:

```text
http://127.0.0.1:8750/
```

Expected result: the `Map Prototype` panel appears with a Canvas map and diagnostics.

Map checks:

1. `Classification` should match the active vessel's orbit state.
2. `Reference Body` should match the orbit reference body.
3. `Scale` should show a meters-per-pixel equivalent.
4. `Projection` should show the active 2D projection mode, currently `orbitPlane`.
5. `Vessel / Conic Residual` should show a distance when a live vessel and sampled conic are both available.
6. For a stable orbiting vessel, the map should draw a body, atmosphere if applicable, closed sampled conic, apoapsis marker, periapsis marker, and vessel marker.
7. For an eccentric or inclined orbit, the map should remain stable and the diagnostic fields should remain populated.
8. For `HyperbolicEscape`, the map should draw an open, truncated trajectory without closing the path.
9. For suborbital trajectories, the map should clearly report that the trajectory intersects the body and draw only drawable above-surface segments.
10. For `Prelaunch`, `Landed`, or `Splashed`, the map should show body context and a clear non-orbital/degraded message instead of a fake stable orbit.
11. `Patch Route` should show patch count, chain status, next transition, encounter diagnostics, and route cards when `orbitPatches[]` is available.
12. Cross-reference-body patches should remain local-route diagnostics in the map prototype and appear as current-time reference-body anchors in the solar-system panel.
13. Browser developer console should stay free of app errors during normal map rendering.

## Solar System Frame Test

In flight, open:

```text
http://127.0.0.1:8750/
```

Expected result: the `Solar System Frame` panel appears with a Canvas map and diagnostics.

Solar-frame checks:

1. `Root Frame` should show `solarSystemRootCenteredInertial`.
2. `Root Body` should normally show `Sun`.
3. `Projection` should show `rootXZ`.
4. Stable Kerbin orbit should show Sun, Kerbin, vessel marker, and visible SOI context when scale allows it.
5. Kerbin escape should show route anchors for the current reference body and the next patched-conic context when KSP exposes it.
6. Duna encounter routes should place the encounter body anchor at the sampled `closestEncounterUniversalTimeSeconds`, not at Duna's current position.
7. Mun/Minmus contexts should include chain bodies in `ephemerisSamples[]` at the current universal time.
8. Leaving flight or closing KSP should retain the last good dashboard data or show disconnected state without browser errors.

## Ephemeris And Solar Trajectories Test (Phase 9)

After build/install, confirm schema v6:

```powershell
Invoke-RestMethod http://127.0.0.1:8750/api/telemetry | Select-Object schemaVersion, ephemerisCaptureStatus, ephemerisValidationResidualMeters
```

Expected: `schemaVersion` is `6`. `ephemerisCaptureStatus` is `ok` or `partial` when patches exist.

Scenario checks in flight:

| Scenario | Expected |
|----------|----------|
| Stable Kerbin orbit | Scrubber spans patch times; start≈end≈now; local map unchanged |
| Kerbin escape | Sun-patch anchor at escape UT; route shows time-keyed anchors |
| Duna encounter | Encounter sample at encounter UT; `Route Overlay` shows patched-conic route when samples succeed |
| Mun/Minmus | Chain bodies listed in ephemeris samples |
| Invalid/missing orbit | `ephemerisCaptureStatus: partial`; fallback placement warning |
| Schema v5 browser cache | Dashboard shows schema mismatch until hard refresh after install |

Solar panel checks:

1. `Ephemeris` diagnostic shows `ok`, `partial`, or `unsupported`.
2. `Route Overlay` shows `patched-conic route (KSP prediction)` when multi-sample placement succeeds.
3. Ephemeris UT scrubber previews nearest samples; `Live` returns to simulation time without controlling KSP.
4. Vessel path (gold dotted) appears on the active patch when `rootPathSamples[]` is populated.
5. Approximate solar conics (cyan dashed) appear when patch-start placement samples exist.

Hard-refresh the dashboard after install (`Ctrl+F5` or `?phase9=1`) so the browser loads schema v6 JavaScript.

## Interactive Solar Map Test (Phase 8)

In flight, open:

```text
http://127.0.0.1:8750/
```

Interactive checks:

1. Mouse wheel zoom should zoom in/out around the cursor without resetting the canvas size.
2. Dragging the solar canvas should pan the view and the view should stay put across telemetry polls.
3. Double-click or `Reset View` should recenter the active camera mode.
4. Camera buttons should switch between full system, vessel, reference body, encounter body, and route modes.
5. `Route Overlay` should show `patched-conic route (KSP prediction)` or `current-frame approximate route` depending on placement mode.
6. Clicking Sun, Kerbin, Duna, vessel, or patch anchors should populate the selection panel.
7. Hovering objects should update the `Hovered` diagnostic without browser console errors.
8. Kerbin escape with Duna encounter should show route anchors and dashed route lines without drawing fake future curved interplanetary geometry in the solar frame.
9. The local `Map Prototype` panel should still render exact/current conic geometry for the active patch.

Negative checks:

```text
http://127.0.0.1:8750/not-found
http://127.0.0.1:8750/../KSP.log
```

Expected result: `404 Not Found`.

After leaving the flight scene, the port should be released. Re-entering flight should start the server again.

## Phase 10 WebGL verification

In flight, open `http://127.0.0.1:8750/`. Confirm `GET /assets/ksp-solar-map.js` returns 200.

| Scenario | Pass |
|----------|------|
| Stable Kerbin orbit | 3D bodies, SOI, active patch conic, vessel marker; Map Prototype unchanged |
| Kerbin escape | Multi-patch arcs + time-keyed route (patched-conic label) |
| Duna encounter | Encounter marker at sampled UT, not current Duna position |
| Camera modes | Full system / vessel / reference / encounter / route; no jump on 1 Hz poll |
| Scrubber | Read-only UT preview; KSP time unchanged |
| Schema v5 JSON | Schema mismatch banner; no silent 3D render |
| 2D toggle | HUD View → 2D Canvas shows legacy solar map |

Truth banner must state: **KSP patched-conic prediction — not SPICE/N-body**.

## Logs

KSP writes `KSP.log` in the KSP install folder. The plugin uses the `[KspWebMap]` prefix for startup, shutdown, and dev-button logs.

Expected smoke-test log markers:

```text
[KspWebMap] Starting service: Dev Window
[KspWebMap] Starting service: Telemetry Snapshot
[KspWebMap] Starting service: Local HTTP Server
[KspWebMap] Local HTTP server listening on http://127.0.0.1:8750/
[KspWebMap] Flight addon loaded.
[KspWebMap] Hello World button clicked. Count: 1
[KspWebMap] Stopping service: Local HTTP Server
[KspWebMap] Local HTTP server stopped.
[KspWebMap] Stopping service: Telemetry Snapshot
[KspWebMap] Stopping service: Dev Window
[KspWebMap] Flight addon destroyed.
```

## Troubleshooting

- If build fails with missing KSP assemblies, verify the `-KspRoot` path points to the folder containing `KSP_x64.exe`.
- If install fails with a missing staged package, run `scripts/build.ps1` first.
- If the dev window does not appear, verify `GameData/KspWebMap/Plugins/KspWebMap.dll` exists in the KSP install.
- If multiple windows appear after scene changes, inspect logs for duplicate addon warnings.
- If the browser cannot connect, verify KSP is currently in a flight scene and inspect `KSP.log` for local server startup errors.
- If port `8750` is already in use, close the other process or change the server configuration in code for this phase.
- If telemetry returns `valid: false`, verify a vessel is active in the flight scene.
- If telemetry appears stale, leave and re-enter flight and inspect `KSP.log` for telemetry capture errors.
