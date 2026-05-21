import type { MapContext } from "../MapContext";
import { buildStarMarkerSegments } from "../elements/starMarker/buildStarMarkerSegments";
import type { MapElementKind, TrajectorySegment } from "../types";

export function buildSegments(
  ctx: MapContext,
  kind: MapElementKind,
): TrajectorySegment[] {
  switch (kind) {
    case "starMarker":
      return buildStarMarkerSegments(ctx);
    default:
      return [];
  }
}
