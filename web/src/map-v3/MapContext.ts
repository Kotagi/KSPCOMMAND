/** V3 map context — Phase 0 delegates to v2 builder; diverge here when v3 planners split. */
export {
  buildMapContext,
  starBody,
  isPlanetBody,
  isMoonBody,
  type MapContext,
  type BodyEntry,
} from "../map-v2/MapContext";
