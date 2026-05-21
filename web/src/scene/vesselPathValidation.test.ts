import { describe, expect, it } from "vitest";
import { isRenderableVesselRootPath } from "./vesselPathValidation";

describe("isRenderableVesselRootPath", () => {
  it("rejects mixed heliocentric and SOI-scale samples (wedge)", () => {
    const points = [
      { x: -2.29e9, y: 0, z: 1.34e10 },
      { x: -2.48e7, y: 98022, z: 7.08e6 },
      { x: -6.52e7, y: 83390, z: 1.39e7 },
    ];
    expect(isRenderableVesselRootPath(points)).toBe(false);
  });

  it("accepts consistent heliocentric samples", () => {
    const points = [
      { x: -2.29e9, y: 0, z: 1.34e10 },
      { x: -2.28e9, y: 1e5, z: 1.341e10 },
      { x: -2.27e9, y: 2e5, z: 1.342e10 },
    ];
    expect(isRenderableVesselRootPath(points)).toBe(true);
  });

  it("accepts long heliocentric chords along one patch (not a wedge)", () => {
    const points = [
      { x: -2.29e9, y: 0, z: 1.34e10 },
      { x: 2.1e9, y: 0, z: -1.2e10 },
      { x: -2.0e9, y: 0, z: 1.3e10 },
    ];
    expect(isRenderableVesselRootPath(points)).toBe(true);
  });
});
