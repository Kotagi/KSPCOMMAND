# Web dev tools

## Planet texture + orientation lab

Isolated full-screen sphere for testing exported body JPEGs, `TexturedPlanetBody` material binding, and **Phase 3.4 orientation POC** (tilt + spin). Not part of the main map bundle.

### Local (Vite)

From `web/`:

```bash
npx vite web/dev/planet-texture-lab.html
```

Open the URL Vite prints (port 5173).

### Texture params

| Param | Example | Purpose |
|-------|---------|---------|
| `textureUrl` | `/assets/bodies/Kerbin.jpg` | Path served by KSP HTTP or `web/public` |
| `rev` | `4f6cdfba` | Cache-bust revision (optional) |

### Orientation params (Phase 3.4 POC)

| Param | Example | Purpose |
|-------|---------|---------|
| `orientation` | `tilt-x-90` | Preset (see below) |
| `spin` | `1` | Animate spin using preset angular velocity |
| `qx`,`qy`,`qz`,`qw` | `0,0,0,1` | Custom KSP-root quaternion (`orientation=custom`) |
| `avx`,`avy`,`avz` | `0,0.00029,0` | Angular velocity rad/s in KSP root (`custom` + `spin=1`) |
| `body` | `Kerbin` | With `orientation=telemetry` — load from `/api/telemetry` |
| `angle` | `1.57` | Initial spin angle rad (`orientation=spin-y`) |

**Presets:** `identity`, `tilt-x-45`, `tilt-x-90`, `obliquity-23`, `spin-y`, `tilt-and-spin`, `custom`, `telemetry`

**Example URLs (Vite, KSP running for texture):**

```text
?orientation=tilt-x-90
?orientation=tilt-and-spin&spin=1
?orientation=obliquity-23
?orientation=telemetry&body=Kerbin
?orientation=custom&qx=0&qy=0&qz=0&qw=1&avy=0.00029&spin=1
```

### In-game (packaged)

After `scripts/build.ps1` + install:

`http://127.0.0.1:8750/planet-texture-lab.html?orientation=tilt-and-spin&spin=1`

Requires a loaded flight for JPEG export under `Web/assets/bodies/`.

### Expected visuals

| Appearance | Meaning |
|------------|---------|
| Blue/green Kerbin | Texture OK |
| Yellow dot offset from pole | Tilt preset applied (north pole marker) |
| Surface features rotating | `spin=1` or `tilt-and-spin` |
| Teal sphere | Loading / no texture URL |
| Flat white | Map not bound (regression) |

**Telemetry mode:** `?orientation=telemetry&body=Kerbin` — requires schema **v10** DLL + loaded flight. Falls back to `tilt-and-spin` with a HUD warning if orientation fields are missing.

Uses the same stack as the main map: `PlanetBodyOrientedGroup` → `PlanetBodyMeshPoleFrame` → `TexturedPlanetBody`.

### Automated tests

```bash
cd web
npm test -- kspBodyOrientation labOrientationConfig planetBodyOrientationFields
```

| Doc | Purpose |
|-----|---------|
| [`docs/MAP_V3_PHASE3_GUIDE.md`](../../docs/MAP_V3_PHASE3_GUIDE.md) | Full Phase 3 how-to + §13 lessons learned |
| [`docs/MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md`](../../docs/MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md) | Tilt/spin spec |
| [`docs/MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](../../docs/MAP_V3_PLANET_BODY_TEXTURE_SPEC.md) | Texture spec §10a |
