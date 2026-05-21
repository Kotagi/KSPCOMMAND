import { describe, expect, it } from "vitest";
import {
  pathEndpointsClose,
  shouldCloseOrbitPeriodTrail,
} from "./pathSegments";
import type { Vector3 } from "../telemetry/schema-v6";

function circlePoints(radius: number, count: number): Vector3[] {
  const points: Vector3[] = [];
  for (let i = 0; i < count; i++) {
    const angle = (Math.PI * 2 * i) / count;
    points.push({
      x: radius * Math.cos(angle),
      y: 0,
      z: radius * Math.sin(angle),
    });
  }
  return points;
}

describe("shouldCloseOrbitPeriodTrail", () => {
  it("closes Jool-scale trails where fixed 5 Gm tolerance fails", () => {
    const joolRadius = 6.9e10;
    const points = circlePoints(joolRadius, 48);
    expect(pathEndpointsClose(points, 5e9)).toBe(false);
    expect(shouldCloseOrbitPeriodTrail(points)).toBe(true);
  });

  it("still closes Kerbin-scale trails", () => {
    const points = circlePoints(1.15e10, 48);
    expect(shouldCloseOrbitPeriodTrail(points)).toBe(true);
  });
});
