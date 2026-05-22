import { describe, expect, it } from "vitest";
import {
  PLANET_BODY_ICON_LOD_SCREEN_RADIUS,
  PLANET_BODY_ICON_RADIUS,
  resolvePlanetBodyDrawMode,
} from "./planetBodyLod";

describe("planetBodyLod", () => {
  it("uses mesh when projected radius is above threshold", () => {
    const meshR = 0.05;
    const d = meshR / (PLANET_BODY_ICON_LOD_SCREEN_RADIUS + 0.01);
    expect(resolvePlanetBodyDrawMode({ sceneMeshRadius: meshR, cameraDistance: d })).toBe(
      "mesh",
    );
  });

  it("uses icon when projected radius is at or below threshold", () => {
    const meshR = 0.05;
    const d = meshR / PLANET_BODY_ICON_LOD_SCREEN_RADIUS;
    expect(resolvePlanetBodyDrawMode({ sceneMeshRadius: meshR, cameraDistance: d })).toBe(
      "icon",
    );
  });

  it("at crossover floored mesh is smaller on screen than fixed icon", () => {
    const meshR = 0.05;
    const d = meshR / PLANET_BODY_ICON_LOD_SCREEN_RADIUS;
    const meshApparent = meshR / d;
    const iconApparent = PLANET_BODY_ICON_RADIUS / d;
    expect(meshApparent).toBeLessThan(iconApparent);
  });
});
