import { describe, expect, it } from "vitest";
import {
  findTrailAnchorIndex,
  findTrailAnchorOnPeriod,
  opacityAlongTrailIndex,
  opacityForTrailSegmentDirected,
  rotateTrailToAnchorIndex,
  trailVertexOpacities,
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
