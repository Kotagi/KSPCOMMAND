import { describe, expect, it } from "vitest";
import {
  findTrailAnchorIndex,
  findTrailAnchorOnPeriod,
  opacityAlongTrailIndex,
  opacityForTrailSegmentDirected,
  rotateTrailToAnchorIndex,
  trailVertexOpacities,
  progradeHalfVertexOpacities,
  retrogradeHalfVertexOpacities,
  closedRingHalfGradientOpacities,
  ORBIT_TRAIL_HALF_PROGRADE_BODY,
  ORBIT_TRAIL_HALF_PROGRADE_FAR,
  ORBIT_TRAIL_HALF_RETRO_BODY,
  ORBIT_TRAIL_HALF_RETRO_FAR,
  ORBIT_TRAIL_OPACITY_PROGRADE_AT_ICON,
  ORBIT_TRAIL_OPACITY_TRAILING,
} from "./orbitTrailDirectionStyle";

describe("orbitTrailDirectionStyle", () => {
  it("assigns prograde opacity to segment leaving anchor, solid on segment arriving", () => {
    const n = 48;
    const times = Array.from({ length: n }, (_, i) => 1000 + i);
    expect(
      opacityForTrailSegmentDirected(0, n, 0, times),
    ).toBe(ORBIT_TRAIL_OPACITY_PROGRADE_AT_ICON);
    expect(
      opacityForTrailSegmentDirected(47, n, 0, times),
    ).toBe(ORBIT_TRAIL_OPACITY_TRAILING);
  });

  it("picks anchor vertex nearest icon on period", () => {
    const square: [number, number, number][] = [
      [10, 0, 0],
      [0, 0, 10],
      [-10, 0, 0],
      [0, 0, -10],
      [10, 0, 0],
    ];
    const icon: [number, number, number] = [0, 0, 10];
    const anchorIndex = findTrailAnchorOnPeriod(square, icon, true);
    expect(anchorIndex).toBe(1);
  });

  it("anchor at body: trailing opaque, prograde faint", () => {
    const count = 48;
    expect(opacityAlongTrailIndex(0, count, 0)).toBeCloseTo(
      ORBIT_TRAIL_OPACITY_TRAILING,
      5,
    );
    expect(opacityAlongTrailIndex(1, count, 0)).toBeCloseTo(
      ORBIT_TRAIL_OPACITY_PROGRADE_AT_ICON,
      5,
    );
    expect(opacityAlongTrailIndex(count - 1, count, 0)).toBeCloseTo(
      ORBIT_TRAIL_OPACITY_TRAILING,
      5,
    );
  });

  it("restores anchor opacity on closed duplicate endpoint", () => {
    const opacities = trailVertexOpacities(49, 0, true);
    expect(opacities[0]).toBeCloseTo(ORBIT_TRAIL_OPACITY_TRAILING, 5);
    expect(opacities[48]).toBeCloseTo(ORBIT_TRAIL_OPACITY_TRAILING, 5);
    const times = Array.from({ length: 48 }, (_, i) => 1000 + i);
    expect(opacityForTrailSegmentDirected(12, 48, 0, times)).toBeCloseTo(
      ORBIT_TRAIL_OPACITY_PROGRADE_AT_ICON,
      5,
    );
  });

  it("retrograde half fades 1.0 at body to 0.7 at far end", () => {
    const ops = retrogradeHalfVertexOpacities(64);
    expect(ops[0]).toBeCloseTo(ORBIT_TRAIL_HALF_RETRO_BODY, 5);
    expect(ops[ops.length - 1]).toBeCloseTo(ORBIT_TRAIL_HALF_RETRO_FAR, 5);
  });

  it("closed ring: bold at body, 0.4 at prograde antipode, returns to 1.0 on retro arc", () => {
    const n = 64;
    const half = Math.floor((n - 1) / 2);
    const ops = closedRingHalfGradientOpacities(n, 0);
    expect(ops[0]).toBeCloseTo(ORBIT_TRAIL_HALF_RETRO_BODY, 5);
    expect(ops[half]).toBeCloseTo(ORBIT_TRAIL_HALF_PROGRADE_FAR, 5);
    expect(ops[half + 1]).toBeGreaterThan(ORBIT_TRAIL_HALF_PROGRADE_FAR);
    expect(ops[n - 1]).toBeGreaterThan(ORBIT_TRAIL_HALF_RETRO_FAR);
  });

  it("prograde half fades 0.7 at body to 0.4 at far end", () => {
    const ops = progradeHalfVertexOpacities(64);
    expect(ops[0]).toBeCloseTo(ORBIT_TRAIL_HALF_PROGRADE_BODY, 5);
    expect(ops[ops.length - 1]).toBeCloseTo(ORBIT_TRAIL_HALF_PROGRADE_FAR, 5);
    expect(ops[32]).toBeGreaterThan(ORBIT_TRAIL_HALF_PROGRADE_FAR);
    expect(ops[32]).toBeLessThan(ORBIT_TRAIL_HALF_PROGRADE_BODY);
  });

  it("rotates closed trail so anchor vertex is first", () => {
    const points: [number, number, number][] = [
      [1, 0, 0],
      [0, 0, 1],
      [-1, 0, 0],
      [0, 0, -1],
      [1, 0, 0],
    ];
    const anchor = findTrailAnchorIndex(points, [0, 0, -1]);
    expect(anchor).toBe(3);
    const rotated = rotateTrailToAnchorIndex(points, anchor, true);
    expect(rotated[0]).toEqual([0, 0, -1]);
    expect(rotated[rotated.length - 1]).toEqual([0, 0, -1]);
  });
});
