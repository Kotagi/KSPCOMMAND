import { describe, expect, it } from "vitest";
import fixture from "../../fixtures/telemetry-kerbin-stable.json";
import type { TelemetrySnapshot } from "../telemetry/schema-v6";
import { buildMapContext } from "./MapContext";
import { buildSegments, buildAllSegments } from "./TrajectoryPlanner";

const snap = fixture as TelemetrySnapshot;

describe("TrajectoryPlanner", () => {
  it("builds star marker at root", () => {
    const ctx = buildMapContext(snap);
    expect(ctx).not.toBeNull();
    const segs = buildSegments(ctx!, "StarMarker");
    expect(segs.length).toBe(1);
    const pos = segs[0].points[0];
    expect(Math.hypot(pos.x, pos.y, pos.z)).toBeLessThan(1e6);
  });

  it("builds planet orbits for heliocentric bodies", () => {
    const ctx = buildMapContext(snap);
    const segs = buildSegments(ctx!, "BodyOrbit", { planetOnly: true });
    expect(segs.length).toBeGreaterThan(0);
    segs.forEach((s) => {
      expect(s.points.length).toBeGreaterThanOrEqual(2);
    });
  });

  it("buildAllSegments includes core roles", () => {
    const ctx = buildMapContext(snap);
    const all = buildAllSegments(ctx!);
    const roles = new Set(all.map((s) => s.role));
    expect(roles.has("StarMarker")).toBe(true);
    expect(roles.has("BodyOrbit")).toBe(true);
    expect(roles.has("BodyPosition")).toBe(true);
  });
});
