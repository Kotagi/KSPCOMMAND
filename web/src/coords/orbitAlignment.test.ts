import { describe, expect, it } from "vitest";
import { nearestPointOnPolyline } from "./orbitAlignment";

describe("nearestPointOnPolyline", () => {
  it("returns zero distance when point lies on segment", () => {
    const polyline = [
      { x: 0, y: 0, z: 0 },
      { x: 10, y: 0, z: 0 },
    ];
    const { distanceMeters } = nearestPointOnPolyline({ x: 5, y: 0, z: 0 }, polyline);
    expect(distanceMeters).toBeLessThan(1e-6);
  });

  it("measures perpendicular distance to segment", () => {
    const polyline = [
      { x: 0, y: 0, z: 0 },
      { x: 10, y: 0, z: 0 },
    ];
    const { distanceMeters } = nearestPointOnPolyline({ x: 5, y: 3, z: 0 }, polyline);
    expect(distanceMeters).toBeCloseTo(3, 6);
  });
});
