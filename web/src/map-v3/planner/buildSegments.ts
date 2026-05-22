import type { MapContext } from "../MapContext";
import { buildStarMarkerSegments } from "../elements/starMarker/buildStarMarkerSegments";
import { buildPlanetBodySegments } from "../elements/planetBody/buildPlanetBodySegments";
import { buildPlanetOrbitSegments } from "../elements/planetOrbit/buildPlanetOrbitSegments";
import type { MapElementKind, TrajectorySegment } from "../types";

export function buildSegments(
  ctx: MapContext,
  kind: MapElementKind,
): TrajectorySegment[] {
  switch (kind) {
    case "starMarker":
      return buildStarMarkerSegments(ctx);
    case "planetOrbit":
      return buildPlanetOrbitSegments(ctx);
    case "planetBody":
      return buildPlanetBodySegments(ctx);
    default:
      return [];
  }
}
