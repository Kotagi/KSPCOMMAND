# Web dev tools

## Planet texture lab

Isolated full-screen sphere for testing exported body JPEGs and `TexturedPlanetBody` material binding. Not part of the main map bundle.

### Local (Vite)

From `web/`:

```bash
npx vite web/dev/planet-texture-lab.html
```

Open the URL Vite prints (port 5173). Query params:

| Param | Example | Purpose |
|-------|---------|---------|
| `textureUrl` | `/assets/bodies/Kerbin.jpg` | Path served by KSP HTTP or `web/public` |
| `rev` | `4f6cdfba` | Cache-bust revision (optional) |

Example with KSP running (proxied `/api` and `/assets`):

`http://127.0.0.1:5173/web/dev/planet-texture-lab.html?textureUrl=/assets/bodies/Kerbin.jpg&rev=4f6cdfba`

### In-game (packaged)

After `scripts/build.ps1` + install:

`http://127.0.0.1:8750/planet-texture-lab.html?textureUrl=/assets/bodies/Kerbin.jpg`

Requires a loaded flight so the plugin has exported JPEGs under `Web/assets/bodies/`.

### Expected visuals

| Appearance | Meaning |
|------------|---------|
| Blue/green Kerbin | Texture applied correctly |
| Teal sphere | Loading or no texture URL |
| Flat white | Map not bound (regression) |
| Flat map color | Fallback (`bodyTextureStatus` not `ready`) |

| Doc | Purpose |
|-----|---------|
| [`docs/MAP_V3_PHASE3_GUIDE.md`](../../docs/MAP_V3_PHASE3_GUIDE.md) | Full Phase 3 how-to (§7 lab) |
| [`docs/MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](../../docs/MAP_V3_PLANET_BODY_TEXTURE_SPEC.md) | Formal texture spec §10a |
