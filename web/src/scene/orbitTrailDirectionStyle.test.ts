import { describe, expect, it } from "vitest";
import {
  findTrailAnchorIndex,
  findTrailAnchorOnPeriod,
  opacityAlongTrailIndex,
  opacityForOrbitTailAhead,
  opacityForTrailSegmentDirected,
  rotateTrailToAnchorIndex,
  trailVertexOpacities,
  progradeHalfVertexOpacities,
  retrogradeHalfVertexOpacities,
  closedRingHalfGradientOpacities,
  ORBIT_TRAIL_TAIL_ATTACH,
  ORBIT_TRAIL_TAIL_LEAD,
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

  it("retrograde half is solid 1.0 on every vertex", () => {
    const ops = retrogradeHalfVertexOpacities(64);
    expect(ops.every((a) => a === ORBIT_TRAIL_HALF_RETRO_BODY)).toBe(true);
    expect(ops[ops.length - 1]).toBeCloseTo(ORBIT_TRAIL_HALF_RETRO_FAR, 5);
  });

  it("motion tail: attach 1.0 at body, lead 0.25 one step prograde, linear to 1.0", () => {
    const n = 64;
    expect(opacityForOrbitTailAhead(0, n)).toBeCloseTo(ORBIT_TRAIL_TAIL_ATTACH, 5);
    expect(opacityForOrbitTailAhead(1, n)).toBeCloseTo(ORBIT_TRAIL_TAIL_LEAD, 5);
    expect(opacityForOrbitTailAhead(n - 1, n)).toBeCloseTo(
      ORBIT_TRAIL_TAIL_ATTACH,
      5,
    );
    const mid = opacityForOrbitTailAhead(32, n);
    expect(mid).toBeGreaterThan(ORBIT_TRAIL_TAIL_LEAD);
    expect(mid).toBeLessThan(ORBIT_TRAIL_TAIL_ATTACH);
  });

  it("closed ring uses monotonic tail ramp in prograde order", () => {
    const n = 64;
    const times = Array.from({ length: n }, (_, i) => 1000 + i);
    const ops = closedRingHalfGradientOpacities(n, 0, times);
    expect(ops[0]).toBeCloseTo(ORBIT_TRAIL_TAIL_ATTACH, 5);
    expect(ops[1]).toBeCloseTo(ORBIT_TRAIL_TAIL_LEAD, 5);
    expect(ops[n - 1]).toBeCloseTo(ORBIT_TRAIL_TAIL_ATTACH, 5);
    for (let i = 2; i < n - 1; i += 1) {
      expect(ops[i]).toBeGreaterThanOrEqual(ops[i - 1] - 1e-9);
    }
  });

  it("prograde half starts at tail lead and rises toward attach", () => {
    const ops = progradeHalfVertexOpacities(64);
    expect(ops[0]).toBeCloseTo(ORBIT_TRAIL_TAIL_ATTACH, 5);
    expect(ops[1]).toBeCloseTo(ORBIT_TRAIL_TAIL_LEAD, 5);
    expect(ops[ops.length - 1]).toBeGreaterThan(ORBIT_TRAIL_TAIL_LEAD);
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
