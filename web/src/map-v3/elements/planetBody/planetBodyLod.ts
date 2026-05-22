/**
 * Map V3 planet mesh ↔ fixed map icon (always on in PlanetBodyLayer).
 * V3-owned; tuned toward KSP tracking-map behavior (see v2 parity notes in tests).
 */

/** Below this projected mesh radius (meshR / cameraDistance), draw icon instead. */
export const PLANET_BODY_ICON_LOD_SCREEN_RADIUS = 0.12;

/** Fixed scene-space icon radius for heliocentric planets (not Sun/moons). */
export const PLANET_BODY_ICON_RADIUS = 0.06;

/** Sphere subdivisions — match star smoothness (8×8 icons look boxy on screen). */
export const PLANET_BODY_MESH_SPHERE_SEGMENTS = 32;
export const PLANET_BODY_ICON_SPHERE_SEGMENTS = 24;

export type PlanetBodyDrawMode = "mesh" | "icon";

export interface PlanetBodyLodInput {
  sceneMeshRadius: number;
  cameraDistance: number;
}

export function resolvePlanetBodyDrawMode(
  input: PlanetBodyLodInput,
): PlanetBodyDrawMode {
  const ratio =
    input.sceneMeshRadius / Math.max(input.cameraDistance, 0.01);
  if (ratio <= PLANET_BODY_ICON_LOD_SCREEN_RADIUS) {
    return "icon";
  }
  return "mesh";
}

export function planetBodyIconRadius(): number {
  return PLANET_BODY_ICON_RADIUS;
}
