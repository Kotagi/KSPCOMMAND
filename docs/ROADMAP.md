# Roadmap

## Phase 0: Foundation

Status: complete.

- KSP loads the plugin DLL.
- Flight scene starts a lifecycle host.
- Service registry owns start and stop order.
- Temporary dev UI verifies in-game interaction.
- Build, package, install, and uninstall scripts are explicit.

Exit criteria:

- Clean build succeeds.
- Staged package contains expected files.
- Install places only `GameData/KspWebMap` under the KSP install.
- Smoke test works in a flight scene.
- Re-entering flight does not create duplicate dev windows.

## Phase 1: Local Server Health

Status: complete.

- Add configuration for enablement, bind address, and port.
- Add a localhost-only HTTP service.
- Add `/api/health`.
- Serve the static development shell at `/`.
- Ensure server shutdown occurs during addon destruction.
- Keep KSP API access on the main thread.

Exit criteria:

- Browser can load `http://127.0.0.1:<port>/api/health`.
- Browser can load `http://127.0.0.1:<port>/`.
- KSP scene changes do not leave the port locked.
- Disabling the service prevents the listener from starting.
- Unknown routes and traversal attempts fail safely.

## Phase 2: Telemetry JSON

Status: complete.

- Add main-thread telemetry snapshots for active vessel and celestial bodies.
- Expose read-only JSON endpoints.
- Avoid returning live Unity/KSP objects from server threads.

Candidate endpoints:

```text
GET /api/active-vessel
GET /api/bodies
GET /api/orbit
GET /api/telemetry
```

Exit criteria:

- Telemetry endpoints return schema-versioned JSON from latest snapshots.
- HTTP request handlers do not access KSP or Unity objects.
- Snapshot fields use documented units and invariant numeric formatting.
- Browser polling does not produce log spam or visible KSP hitches.

## Phase 3: Browser UI

Status: complete.

- Consume the Phase 2 telemetry contract.
- Serve static web assets from the staged `Web` folder.
- Add a plain HTML/JavaScript client first.
- Render a basic body/vessel/orbit view from telemetry JSON.

Exit criteria:

- Dashboard loads at `http://127.0.0.1:8750/`.
- Dashboard polls `/api/telemetry` without overlapping requests.
- Dashboard handles disconnected, degraded, stale, schema mismatch, and null-data states.
- Dashboard displays vessel, orbit, body, and diagnostic panels without external dependencies.

## Phase 4: Map Renderer

Status: complete.

- Build on the Phase 3 dashboard shell.
- Add schema v2 map-safe telemetry.
- Add first 2D Canvas active-vessel map prototype.
- Draw reference body, atmosphere, orbit path, apsis markers, vessel marker, labels, and diagnostics.
- Keep the browser renderer independent of KSP's in-game map view.

Exit criteria:

- Dashboard includes a truthful map panel.
- Prelaunch/landed/suborbital/invalid states do not draw misleading stable orbits.
- Stable elliptic orbits render as a closed ellipse with body and apsis markers.
- Existing dashboard, telemetry, health, and route-safety behavior remains intact.

## Phase 5: Expanded Map Semantics

Status: complete.

- Bump telemetry to schema v3 with conic reconstruction fields and minimal patch metadata.
- Generate active-vessel orbit paths from 3D orbital elements.
- Project sampled conic points into the existing 2D Canvas renderer.
- Render elliptic, suborbital, and hyperbolic paths truthfully.
- Keep full patched conics, maneuver nodes, targeting, vessel selection, pan/zoom, and WebGL as later phases.

Exit criteria:

- Dashboard requires schema v3 for map rendering.
- Stable elliptic orbits render as sampled closed conics rather than Canvas ellipse primitives.
- Suborbital paths draw only truthful, degraded above-surface trajectory segments.
- Hyperbolic escapes render as open, bounded paths without fake closure.
- Projection and vessel/conic residual diagnostics are visible in the dashboard.
- Existing telemetry, health, route-safety, and dashboard resilience behavior remains intact.

## Phase 6: Patched Conic Chain Rendering

Status: complete.

- Bump telemetry to schema v4 with bounded `orbitPatches[]` route data.
- Capture KSP's predicted `nextPatch` chain on the Unity main thread.
- Add patch-route diagnostics for current patch, next transition, encounter body, and capture warnings.
- Reuse Phase 5 conic sampling for active and same-reference future local patch previews.
- Preserve the Phase 7 boundary: no unified solar-system/world-frame rendering yet.

Exit criteria:

- Dashboard requires schema v4 for map rendering.
- Stable orbit reports a single active patch.
- Kerbin escape reports future patch transitions without inventing encounter geometry.
- Duna/Mun/Minmus encounters show captured patch-route diagnostics when KSP exposes them.
- Inactive or bogus `nextPatch` objects stop traversal with a visible warning.
- Existing telemetry, health, route-safety, and dashboard resilience behavior remains intact.

## Phase 7: Solar System Frame

Status: complete.

- Bump telemetry to schema v5 with `solarSystemRootCenteredInertial` metadata.
- Add root-relative body positions, body velocities where available, and active-vessel root-frame state.
- Attach each orbit patch to its current reference-body root-frame anchor.
- Draw bodies, SOIs, vessel, and route anchors in one 2D solar-system panel.
- Preserve local conic rendering as the precision diagnostic for active/current patch geometry.

Exit criteria:

- Dashboard requires schema v5 for shared-frame rendering.
- Stable orbit shows Sun/root, Kerbin, vessel, and local patch diagnostics.
- Escape and encounter routes show route anchors without pretending current-time body positions are future encounter positions.
- Schema fields clearly name frame, origin body, units, capture time, and approximation warnings.
- Existing telemetry, health, route-safety, local map, and dashboard resilience behavior remains intact.

## Phase 8: Interactive Map Polish

Status: complete.

- Add pan and zoom to the Solar System Frame panel.
- Add camera modes: full system, active vessel, current reference body, encounter body, and route.
- Add click/hover selection, label decluttering, and selection diagnostics.
- Draw Sun/root body, SOI context, vessel, patch anchors, and current-frame approximate route overlay.
- Keep exact local conic rendering in Map Prototype; defer future-time ephemeris to Phase 9.

Exit criteria:

- Solar map supports wheel zoom, drag pan, double-click/reset recenter, and camera mode buttons.
- User pan/zoom does not reset on every telemetry poll.
- Route overlay is labeled approximate and uses current-time patch anchors only.
- Stable orbit, Kerbin escape, and Duna encounter scenarios remain readable in solar and local views.
- Existing schema v5 telemetry, health, route-safety, and dashboard resilience behavior remains intact.

## Phase 9: Future Ephemeris And Solar Trajectories

Status: complete.

- Bump telemetry to schema v6 with `ephemerisSamples[]`, per-patch `placementSamples[]`, and `ephemerisCaptureStatus`.
- Sample reference and encounter body root-frame positions at patch start, end, and closest-encounter universal times using KSP orbit propagation (`getTruePositionAtUT` with Y/Z flip handling).
- Draw time-keyed route polylines, approximate solar-frame conic segments translated by sampled patch placement, and active-patch vessel root path samples.
- Add a read-only ephemeris UT scrubber (preview only; does not control KSP).
- Label truth rules explicitly: encounter anchors use sampled encounter UT, not current body position. This is KSP patched-conic prediction, not SPICE/N-body integration.

Exit criteria:

- Dashboard requires schema v6 for solar-system ephemeris rendering (schema v5 shows a clear mismatch).
- Duna encounter route places the encounter body anchor at `closestEncounterUniversalTimeSeconds`, not at the body's current position.
- Route overlay connects time-ordered patch anchors when `patchPlacementMode` is `multiSampleEphemeris`.
- Diagnostics distinguish current-frame body dots from sampled placement and report `ephemerisCaptureStatus`.
- Phases 7–8 interactive solar behavior (pan, zoom, camera modes, selection) remains intact without WebGL.

## Phase 10: WebGL Solar Renderer

Status: complete (MVP shipped; polish in Phase 10.5).

- Add `web/` Vite + TypeScript + React + React Three Fiber bundle; ship `dist/` into `GameData/KspWebMap/Web/`.
- Extract `buildSolarSystemModel` and `buildConicGeometry` into pure TS modules shared by 2D fallback and 3D renderer.
- Serve static assets under `/assets/*` from the local HTTP server.
- 3D solar map: world-shift, display scale, bodies, SOI, patch conics, patched-conic route, vessel path, camera modes, ephemeris scrubber, truth HUD.
- Schema v7 (optional): `bodyOrbitPaths[]` for celestial body orbit trails in 3D.
- Defer maneuver nodes, other vessels UI, and browser-to-KSP control.

Exit criteria:

- Dashboard requires schema v6 for 3D solar rendering (schema mismatch banner otherwise).
- Stable orbit, Kerbin escape, and Duna encounter scenarios match Phase 9 truth rules in 3D.
- Camera modes and scrubber work without resetting on telemetry poll.
- `scripts/build.ps1` runs `npm run build` and packages web assets; players do not run npm.
- Map Prototype (local 2D conic) remains functional.

## Phase 10.5: 3D Solar Map Polish

Status: complete.

- Unify 3D controls under React `MapHud`; hide legacy `#solarControls` when in 3D mode.
- Extend `KspSolarMap` API (camera, scrub, recenter, selection, `getModel`).
- Port camera framing from Phase 8; vessel interpolation between polls.
- Add 3D selection, labels, atmosphere shell, apsis markers, SOI LOD, world-shift focus.
- Quality presets (Low/Med/High); line decimation; minimal Sun/Kerbin textures.
- Flight verification matrix; mark Phase 10 exit criteria satisfied.

Exit criteria:

- No duplicate scrub/camera controls in 3D default view.
- Selection updates `#solarSelection`; labels visible for major bodies.
- Camera modes frame content; recenter/reset in HUD; no jump on 1 Hz poll.
- One `buildSolarSystemModel` per poll in 3D mode.
- LKO / escape / encounter scenarios pass OPERATIONS Phase 10.5 checklist.
