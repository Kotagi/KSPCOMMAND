# Architecture

## Current Scope

The current plugin is a hardened smoke-test foundation with localhost server, telemetry JSON, browser dashboard, expanded conic map rendering, patched-conic route diagnostics, a shared solar-system frame, and interactive solar-map polish. It proves that KSP can load the assembly, enter the flight scene, create managed services, respond to an in-game button press, serve loopback-only endpoints, expose read-only telemetry snapshots, present them in a local browser dashboard, render active-vessel conics from 3D orbital elements into a 2D Canvas, report KSP's predicted patch chain, place bodies and route anchors in a common root-centered frame, and let the user pan, zoom, select objects, and switch camera modes in the browser.

Full KSP map-view parity is intentionally not implemented yet.

## Runtime Components

```text
KspWebMapAddon
  Core lifecycle host created by KSP in the flight scene.

ServiceRegistry
  Starts and stops owned services in a predictable order.

DevWindowService
  Owns the temporary Hello World IMGUI component.

HelloWorldWindow
  Temporary flight-scene UI used only to verify KSP interaction.

LocalHttpServerService
  Loopback-only HTTP service for health, telemetry, and static shell routes.

TelemetrySnapshotService
  Main-thread KSP data capture service.

TelemetryStore
  Thread-safe latest-snapshot handoff between capture and HTTP serialization.

BrowserDashboard
  Self-contained static HTML dashboard served from GameData/KspWebMap/Web.

CanvasMapRenderer
  Browser-side 2D Canvas renderer that samples 3D conic geometry from telemetry snapshots only.

SolarFrameRenderer
  Browser-side 2D Canvas renderer that projects root-frame body, vessel, and patch-anchor positions.
```

## Lifecycle Rules

- KSP creates `KspWebMapAddon` for `KSPAddon.Startup.Flight`.
- The addon protects against duplicate active instances.
- The addon creates a `ServiceRegistry` during `Awake`.
- Services start during `Awake`.
- Services stop in reverse order during `OnDestroy`.
- Service startup rolls back previously started services if a later service fails.
- Long-running services must have explicit shutdown behavior.
- Telemetry capture starts before the HTTP server so endpoints can serve an explicit latest state.

## Threading Rules

Unity and KSP APIs are not thread-safe. Future web server request handlers must not call KSP or Unity APIs directly from background threads.

The intended pattern is:

1. Collect KSP state on Unity's main thread.
2. Convert that state into plain immutable telemetry snapshots.
3. Let server code read those snapshots without touching Unity objects.
4. Use a main-thread bridge only when background work must request Unity-side behavior.

The current telemetry implementation follows this boundary: `TelemetrySnapshotService` reads KSP objects on the Unity main thread, publishes DTO snapshots, and `LocalHttpServerService` serializes only those DTOs.

## Localhost Policy

The server binds to `127.0.0.1:8750` by default. It must not listen on all interfaces unless a deliberate configuration option is added and documented.

The server exposes only:

```text
GET /api/health
HEAD /api/health
GET /api/telemetry
HEAD /api/telemetry
GET /api/active-vessel
HEAD /api/active-vessel
GET /api/orbit
HEAD /api/orbit
GET /api/bodies
HEAD /api/bodies
GET /
HEAD /
GET /index.html
HEAD /index.html
```

Telemetry endpoints use schema version `7` (dashboard accepts `>= 6`) and explicit unit suffixes such as `Meters`, `MetersPerSecond`, and `Seconds`.

Schema v6 extends v5 with ephemeris and time-sampled placement fields:

```text
ephemerisCaptureStatus
ephemerisValidationResidualMeters
ephemerisSamples[]
ephemerisSamples[].targetBody
ephemerisSamples[].sampleRole
ephemerisSamples[].sampleUniversalTimeSeconds
ephemerisSamples[].positionRootRelativeMeters
ephemerisSamples[].velocityRootRelativeMetersPerSecond
ephemerisSamples[].referenceFrame
ephemerisSamples[].sampleSource
ephemerisSamples[].sampleWarning
orbitPatches[].placementSamples[]
orbitPatches[].placementSamples[].sampleRole
orbitPatches[].placementSamples[].targetBody
orbitPatches[].placementSamples[].sampleUniversalTimeSeconds
orbitPatches[].placementSamples[].positionRootRelativeMeters
orbitPatches[].closestApproachMeters
bodies[].positionSampleUniversalTimeSeconds
activeVessel.rootPathSamples[]
activeVessel.rootPathSamples[].sampleUniversalTimeSeconds
activeVessel.rootPathSamples[].positionRootRelativeMeters
bodyOrbitCaptureStatus
bodyOrbitPaths[]
bodyOrbitPaths[].bodyName
bodyOrbitPaths[].referenceBody
bodyOrbitPaths[].samples[]
bodyOrbitPaths[].samples[].sampleUniversalTimeSeconds
bodyOrbitPaths[].samples[].positionRootRelativeMeters
```

Schema v5/v6/v7 shared map-safe conic, patch-chain, and shared-frame fields:

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

Local conic positions use `orbitReferenceBodyCenteredInertial`. Shared solar-system positions use `solarSystemRootCenteredInertial`, centered on the captured root body at the telemetry capture universal time.

## Browser Dashboard

The dashboard shell is [GameData/KspWebMap/Web/index.html](GameData/KspWebMap/Web/index.html) with inline CSS and JavaScript for metrics, Map Prototype (2D local conic), and polling. Phase 10 adds a Vite-built WebGL solar map bundle at `Web/assets/ksp-solar-map.js`, mounted via `window.KspSolarMap`.

The local HTTP server serves `/`, `/index.html`, and static files under `/assets/*` (JS, CSS, fonts, images).

Dashboard behavior:

- Polls `GET /api/telemetry` at 1 Hz while visible.
- Slows polling while the browser tab is hidden.
- Prevents overlapping requests.
- Times out slow requests.
- Retains last good telemetry during temporary disconnects.
- Displays connected, degraded, disconnected, stale, and schema mismatch states.
- Includes a Canvas map renderer that samples conic geometry from orbital elements.
- Includes patch-route diagnostics from KSP's predicted `nextPatch` chain.
- Includes a **3D Solar System Frame** (React Three Fiber) with world-shift, display scale, bodies, SOI, patch conics, patched-conic route, vessel path, camera modes, and truth HUD.
- Retains a **2D Canvas fallback** for the solar panel (toggle in the 3D HUD).
- Does not add WebSockets, maneuver nodes, or browser-to-KSP control.
- Adds read-only ephemeris UT scrubbing for sampled placement preview (does not advance KSP time).

### Web UI source (`web/`)

```text
web/src/model/buildSolarSystemModel.ts   Pure telemetry → view model (no WebGL)
web/src/math/buildConicGeometry.ts       Kepler sampling (shared with Map Prototype logic)
web/src/scene/Map3D.tsx                  R3F scene and layers
web/src/mount.tsx                        window.KspSolarMap embed API
```

`scripts/build.ps1` runs `npm run build` and copies `web/dist/assets/*` into the staged package.

## Map Renderer

The map renderer is browser-only. It consumes schema v6+ telemetry and never accesses KSP directly.

The renderer generates conic points in a perifocal orbital plane, rotates them into the reference-body inertial frame using longitude of ascending node, inclination, and argument of periapsis, then projects those points to the 2D Canvas. The default projection mode is `orbitPlane` so the conic shape remains readable while the math foundation is validated. Diagnostic fields show the active projection and the closest sampled conic distance to the live vessel position when available.

Phase 6 adds route diagnostics from `orbitPatches[]`. Phase 7 adds root-frame placement metadata and a Solar System Frame panel. Phase 9 samples body positions at patch event universal times and draws time-keyed solar routes, approximate translated conics, and vessel path samples. This follows patched-conic mission-design practice (departure/cruise/arrival segments patched at SOI boundaries with bodies placed at chosen dates) but is not full SPICE ephemeris integration.

## Shared Solar-System Frame

The root frame is named `solarSystemRootCenteredInertial`. The capture service identifies a root body, normally `Sun`, then stores body positions as `body.position - rootBody.position` and active-vessel position as `vessel.GetWorldPos3D() - rootBody.position`. Body and vessel velocities are captured relative to the same root where KSP exposes frame velocity data.

Patch placement uses schema v6 ephemeris samples when capture succeeds. `orbitPatches[].placementSamples[]` records reference and encounter body root-frame positions at patch start, end, and closest-encounter universal times. `referenceBodyPositionRootRelativeMeters` defaults to the patch-start sample for route anchoring. When sampling fails, the service falls back to `patchPlacementMode: currentReferenceBodyPosition` with an explicit warning.

Body propagation uses `Orbit.getTruePositionAtUT` with KSP's documented Y/Z flip applied in one helper. A validation pass at capture time compares propagated positions to current `body.position` and reports `ephemerisValidationResidualMeters`.

## Interactive Solar Map (Phases 8–10)

The Solar System Frame panel requires schema v6+ telemetry.

**3D (Phase 10–10.5, default):** React Three Fiber scene with `OrbitControls`, world-shift focus (`focusBodyName` on reference/encounter modes), display-scale compression, camera bounds fitting (`solarCameraBounds.ts`), vessel interpolation between polls, raycast selection, screen labels, atmosphere shells, Pe/Ap markers, SOI distance LOD, quality presets (Low/Med/High), and layers for bodies (procedural Sun/Kerbin textures), patch conics (decimated), route polyline, placement markers, vessel marker/path, and optional `bodyOrbitPaths[]` (schema v7).

**Shell integration:** `window.KspSolarMap` exposes `setCameraMode`, `setScrub*`, `recenter`, `resetView`, `getModel`, `onSelectionChange`. In 3D mode the legacy `#solarControls` panel is hidden; `MapHud` is the sole control surface. `index.html` uses `getModel()` once per poll (no duplicate `buildSolarSystemModel`).

**2D fallback:** Legacy Canvas renderer in `index.html` (`solarViewState`) — wheel zoom, drag pan, camera modes, route overlay, translated conics, vessel path, scrubber highlights.

Truth rules (NASA-aligned patched-conic context; not SPICE/N-body):

| Display | Allowed when | Label |
|---------|----------------|-------|
| Current-time body dots | Always | current frame |
| Patch anchor at patch start UT | Sample succeeded | patch start |
| Encounter anchor | `closestEncounterUniversalTimeSeconds` sampled | encounter (sampled UT) |
| Route polyline | ≥2 successful samples | patched-conic route (KSP prediction) |
| Solar conic arc | Patch-start sample + conic elements | conic in reference SOI; body at sampled UT |
| Straight chord fallback | Sample missing | current-frame approximate + warning |

References: [NASA Basics of Space Flight — Trajectories](https://science.nasa.gov/learn/basics-of-space-flight/chapter4-1/), [NASA PatCon SOI patching (AAS 07-160)](https://ntrs.nasa.gov/api/citations/20070010447/downloads/20070010447.pdf).

Schema v7 adds `bodyOrbitPaths[]` — root-frame samples of major celestial orbits for faint planet trail lines in 3D.

Rendering rules:

- `Prelaunch`, `Landed`, and `Splashed`: draw body context and a clear non-orbital/degraded message.
- `Suborbital`: draw an explicitly degraded/intersecting-body trajectory.
- `Elliptic`: draw a closed sampled conic, reference body, atmosphere, apsis markers, and vessel marker.
- `HyperbolicEscape`: draw an open sampled conic truncated before asymptotes and bounded by SOI or display limits.
- `Invalid`: do not draw misleading geometry.

## Source Boundaries

```text
src/KspWebMap/Core
  Addon entry point, lifecycle host, service registry, shared contracts.

src/KspWebMap/DevTools
  Temporary development-only UI and smoke-test helpers.

src/KspWebMap/Configuration
  Future settings such as bind address, port, enabled flags, and diagnostics.

src/KspWebMap/Services
  Long-running services such as the local HTTP server.

src/KspWebMap/Telemetry
  KSP data extraction, snapshot models, store, and JSON serialization.
```
