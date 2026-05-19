import { describe, expect, it } from "vitest";
import {
  buildConicGeometry,
  calculateSemiLatusRectum,
} from "./buildConicGeometry";
import type { OrbitElements } from "../telemetry/schema-v6";

describe("buildConicGeometry", () => {
  it("calculates semi-latus rectum for circular orbit", () => {
    const p = calculateSemiLatusRectum(7000000, 0);
    expect(p).toBeCloseTo(7000000, 0);
  });

  it("samples closed ellipse", () => {
    const orbit: OrbitElements = {
      eccentricity: 0.1,
      semiMajorAxisMeters: 7000000,
      semiLatusRectumMeters: 6930000,
      longitudeOfAscendingNodeDegrees: 0,
      inclinationDegrees: 0,
      argumentOfPeriapsisDegrees: 0,
      referenceBodyRadiusMeters: 600000,
    };
    const geom = buildConicGeometry(orbit, 600000, "orbitPlane", true);
    expect(geom.canDraw).toBe(true);
    expect(geom.points.length).toBeGreaterThan(50);
    expect(geom.segments.length).toBeGreaterThan(0);
  });

  it("rejects invalid eccentricity", () => {
    const geom = buildConicGeometry({ eccentricity: -1 }, 600000);
    expect(geom.canDraw).toBe(false);
  });
});
