import { describe, expect, it } from "vitest";
import {
  BODY_ORBIT_TRAIL_PERIOD_VERTICES,
  densifyOrbitTrailPoints,
  densifySampleUniversalTimes,
} from "./densifyOrbitTrail";

describe("densifyOrbitTrail", () => {
  it("increases closed orbit vertex count beyond telemetry samples", () => {
    const square: [number, number, number][] = [
      [10, 0, 0],
      [0, 0, 10],
      [-10, 0, 0],
      [0, 0, -10],
      [10, 0, 0],
    ];
    const dense = densifyOrbitTrailPoints(square, true, 64);
    expect(dense.length).toBeGreaterThan(square.length);
    expect(dense.length - 1).toBeGreaterThanOrEqual(64);
    expect(dense.length - 1).toBeLessThanOrEqual(512);
    expect(dense[0]).toEqual(dense[dense.length - 1]);
  });

  it("densifies closed loop without duplicate closing vertex", () => {
    const ring: [number, number, number][] = [
      [10, 0, 0],
      [0, 0, 10],
      [-10, 0, 0],
      [0, 0, -10],
    ];
    const dense = densifyOrbitTrailPoints(ring, false, 64, true);
    expect(dense.length).toBe(64);
  });

  it("interpolates sample times to densified period length", () => {
    const times = Array.from({ length: 48 }, (_, i) => 1000 + i * 100);
    const dense = densifySampleUniversalTimes(times, BODY_ORBIT_TRAIL_PERIOD_VERTICES);
    expect(dense.length).toBe(BODY_ORBIT_TRAIL_PERIOD_VERTICES);
    expect(dense[0]).toBeCloseTo(1000, 5);
    expect(dense[1]).toBeGreaterThan(dense[0]);
  });
});
