import type { MapElementKind } from "./types";

/** Which v3 element layers are active — enable one phase at a time. */
export type MapV3LayerFlags = Record<MapElementKind, boolean>;

/** Phase 0: scaffold only — no elements drawn. */
export const MAP_V3_LAYERS_PHASE0: MapV3LayerFlags = {
  starMarker: false,
  planetOrbit: false,
  planetBody: false,
  moonOrbit: false,
  moonBody: false,
  vesselMarker: false,
  bodyLod: false,
  vesselOrbit: false,
  bodyLabel: false,
  futureRoute: false,
  soiRing: false,
  selection: false,
};

/** Phase 1: primary star at system center. */
export const MAP_V3_LAYERS_PHASE1: MapV3LayerFlags = {
  ...MAP_V3_LAYERS_PHASE0,
  starMarker: true,
};

/** Phase 2: star + heliocentric planet orbit paths only. */
export const MAP_V3_LAYERS_PHASE2: MapV3LayerFlags = {
  ...MAP_V3_LAYERS_PHASE1,
  planetOrbit: true,
};

/** Phase 3.1: star + planet orbits + planet bodies (mesh/icon LOD). */
export const MAP_V3_LAYERS_PHASE3: MapV3LayerFlags = {
  ...MAP_V3_LAYERS_PHASE2,
  planetBody: true,
};

export const MAP_V3_PHASE_LABEL = "v3 phase 3.1 — planet bodies";
