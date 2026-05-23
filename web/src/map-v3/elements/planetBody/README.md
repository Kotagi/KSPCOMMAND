# `planetBody` element (Map V3)

| Layer | Builder | Policy |
|-------|---------|--------|
| `PlanetBodyLayer` | `buildPlanetBodySegments` | `planetBodyLod.ts` |

## Data flow

```text
telemetry.bodies[] → MapContext.bodyByName
  → buildPlanetBodySegments (one point per planet)
  → PlanetBodyLayer (visibility filter)
  → PlanetBodyMesh (LOD + texture)
```

## Modules

| File | Role |
|------|------|
| `buildPlanetBodySegments.ts` | Segment planner — position only |
| `planetBodyLod.ts` | Mesh vs fixed-screen dot crossover |
| `planetBodyTextureFields.ts` | Telemetry texture URL/status helpers |
| `scene/v3/layers/PlanetBodyMesh.tsx` | LOD router |
| `scene/v3/layers/PlanetBodyDot.tsx` | Icon LOD |
| `scene/v3/layers/TexturedPlanetBody.tsx` | Mesh LOD + exported JPEG |
| `scene/v3/layers/FlatPlanetBody.tsx` | Mesh LOD color fallback |
| `assets/planetBodyTextures.ts` | TextureLoader + cache |

| Doc | Purpose |
|-----|---------|
| [`docs/MAP_V3_PHASE3_GUIDE.md`](../../../../docs/MAP_V3_PHASE3_GUIDE.md) | **Start here** — quick start, textures, troubleshooting |
| [`docs/MAP_V3_PLANET_BODY_SPEC.md`](../../../../docs/MAP_V3_PLANET_BODY_SPEC.md) | Formal body spec (LOD, position) |
| [`docs/MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](../../../../docs/MAP_V3_PLANET_BODY_TEXTURE_SPEC.md) | Formal texture export spec |
| [`web/dev/README.md`](../../../../dev/README.md) | Planet texture lab |
