import { describe, expect, it } from "vitest";
import {
  angleBetweenNormalsDegrees,
  planeNormalFromThreePoints,
} from "./orbitPlaneValidation";

describe("orbitPlaneValidation", () => {
  it("computes unit normal for XY plane", () => {
    const n = planeNormalFromThreePoints(
      { x: 0, y: 0, z: 0 },
      { x: 1, y: 0, z: 0 },
      { x: 0, y: 1, z: 0 },
    );
    expect(n).not.toBeNull();
    expect(n!.z).toBeCloseTo(1, 6);
  });

  it("reports zero angle for parallel normals", () => {
    const a = { x: 0, y: 0, z: 1 };
    expect(angleBetweenNormalsDegrees(a, { x: 0, y: 0, z: -1 })).toBeCloseTo(0, 6);
  });
});
