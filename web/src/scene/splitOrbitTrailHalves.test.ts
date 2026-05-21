import { describe, expect, it } from "vitest";
import { splitOrbitTrailHalves } from "./splitOrbitTrailHalves";
import {
  ORBIT_TRAIL_OPACITY_PROGRADE_AT_ICON,
  ORBIT_TRAIL_OPACITY_TRAILING,
} from "./orbitTrailDirectionStyle";

describe("splitOrbitTrailHalves", () => {
  it("returns two polylines meeting at anchor", () => {
    const ring: [number, number, number][] = Array.from({ length: 64 }, (_, i) => {
      const a = (i / 64) * Math.PI * 2;
      return [Math.cos(a) * 10, 0, Math.sin(a) * 10];
    });
    const closed = [...ring, ring[0]];
    const times = Array.from({ length: 64 }, (_, i) => 1000 + i);
    const { retrograde, prograde } = splitOrbitTrailHalves(
      closed,
      0,
      true,
      times,
    );
    expect(retrograde.points[0]).toEqual(ring[0]);
    expect(prograde.points[0]).toEqual(ring[0]);
    expect(retrograde.opacity).toBe(ORBIT_TRAIL_OPACITY_TRAILING);
    expect(prograde.opacity).toBe(ORBIT_TRAIL_OPACITY_PROGRADE_AT_ICON);
    expect(retrograde.points.length).toBeGreaterThan(16);
    expect(prograde.points.length).toBeGreaterThan(16);
  });
});
