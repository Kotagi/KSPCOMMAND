# planetOrbit element module

Heliocentric planet orbit polylines for Map V3.

| File | Role |
|------|------|
| `planetOrbitStyle.ts` | Line width + color resolver |
| `densifyPlanetOrbitTrail.ts` | Analytic source preference, 512-vertex densify, UT helpers |
| `buildPlanetOrbitSegments.ts` | `TrajectorySegment[]` with `kind: planetOrbit` |

**Orbit trail manual (read first):** [`docs/ORBIT_TRAIL_DRAWING_GUIDE.md`](../../../../docs/ORBIT_TRAIL_DRAWING_GUIDE.md)

**Element spec:** [`docs/MAP_V3_PLANET_ORBIT_SPEC.md`](../../../../docs/MAP_V3_PLANET_ORBIT_SPEC.md)

**Presentation:** `scene/v3/layers/PlanetOrbitLayer.tsx` → `OrbitTrailV3` → `scene/GradientDirectionalOrbitTrail.tsx`
