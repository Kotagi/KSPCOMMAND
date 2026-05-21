import { StarMarkerLayer } from "./layers/StarMarkerLayer";

/**
 * R3F layer composition for Map V3.
 * Layers self-null when their flag is off.
 */
export function MapV3LayerStack() {
  return (
    <>
      <StarMarkerLayer />
    </>
  );
}
