import { StarMarkerLayer } from "./layers/StarMarkerLayer";
import { PlanetOrbitLayer } from "./layers/PlanetOrbitLayer";

/**
 * R3F layer composition for Map V3.
 * Layers self-null when their flag is off.
 */
export function MapV3LayerStack() {
  return (
    <>
      <StarMarkerLayer />
      <PlanetOrbitLayer />
    </>
  );
}
