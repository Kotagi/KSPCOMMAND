import { describe, expect, it } from "vitest";
import {
  PLANET_BODY_DOT_PIXEL_SIZE,
  PLANET_BODY_LOD_DEFAULT_FOV_DEG,
  PLANET_BODY_LOD_DEFAULT_VIEWPORT_HEIGHT,
  planetBodyMeshProjectedDiameterPx,
  resolvePlanetBodyDrawMode,
} from "./planetBodyLod";

describe("planetBodyLod", () => {
  it("uses mesh when projected diameter exceeds dot size", () => {
    const meshR = 0.05;
    const d =
      (meshR * 2 * (PLANET_BODY_LOD_DEFAULT_VIEWPORT_HEIGHT * 0.5)) /
      (Math.tan((PLANET_BODY_LOD_DEFAULT_FOV_DEG * Math.PI) / 360) *
        (PLANET_BODY_DOT_PIXEL_SIZE + 1));
    expect(resolvePlanetBodyDrawMode({ sceneMeshRadius: meshR, cameraDistance: d })).toBe(
      "mesh",
    );
  });

  it("uses dot when projected diameter is at or below dot size", () => {
    const meshR = 0.05;
    const d =
      (meshR * 2 * (PLANET_BODY_LOD_DEFAULT_VIEWPORT_HEIGHT * 0.5)) /
      (Math.tan((PLANET_BODY_LOD_DEFAULT_FOV_DEG * Math.PI) / 360) *
        PLANET_BODY_DOT_PIXEL_SIZE);
    expect(resolvePlanetBodyDrawMode({ sceneMeshRadius: meshR, cameraDistance: d })).toBe(
      "icon",
    );
  });

  it("dev override force icon ignores zoom", () => {
    expect(
      resolvePlanetBodyDrawMode({
        sceneMeshRadius: 0.05,
        cameraDistance: 1,
        devOverride: "icon",
      }),
    ).toBe("icon");
  });

  it("dev override force mesh ignores zoom", () => {
    expect(
      resolvePlanetBodyDrawMode({
        sceneMeshRadius: 0.05,
        cameraDistance: 1000,
        devOverride: "mesh",
      }),
    ).toBe("mesh");
  });

  it("at crossover mesh diameter on screen equals dot diameter", () => {
    const meshR = 0.05;
    const d =
      (meshR * 2 * (PLANET_BODY_LOD_DEFAULT_VIEWPORT_HEIGHT * 0.5)) /
      (Math.tan((PLANET_BODY_LOD_DEFAULT_FOV_DEG * Math.PI) / 360) *
        PLANET_BODY_DOT_PIXEL_SIZE);
    expect(planetBodyMeshProjectedDiameterPx(meshR, d)).toBeCloseTo(
      PLANET_BODY_DOT_PIXEL_SIZE,
      5,
    );
    expect(resolvePlanetBodyDrawMode({ sceneMeshRadius: meshR, cameraDistance: d })).toBe(
      "icon",
    );
  });
});
