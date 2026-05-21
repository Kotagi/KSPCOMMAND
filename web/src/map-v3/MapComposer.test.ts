import { describe, expect, it } from "vitest";
import { composeMapV3Layers } from "./MapComposer";
import { MAP_V3_LAYERS_PHASE0, MAP_V3_LAYERS_PHASE1 } from "./layerFlags";

describe("composeMapV3Layers", () => {
  it("returns no layers for phase 0 flags", () => {
    expect(composeMapV3Layers(MAP_V3_LAYERS_PHASE0)).toEqual([]);
  });

  it("returns StarMarkerLayer only for phase 1 flags", () => {
    expect(composeMapV3Layers(MAP_V3_LAYERS_PHASE1)).toEqual([
      "StarMarkerLayer",
    ]);
  });
});
