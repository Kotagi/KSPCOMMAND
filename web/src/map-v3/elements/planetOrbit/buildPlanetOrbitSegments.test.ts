import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-stable.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildMapContext, isMoonBody } from "../../MapContext";
import { buildPlanetOrbitSegments } from "./buildPlanetOrbitSegments";

const snap = fixture as TelemetrySnapshot;

describe("buildPlanetOrbitSegments", () => {
  it("builds heliocentric planet trails only", () => {
    const ctx = buildMapContext(snap)!;
    const segs = buildPlanetOrbitSegments(ctx);
    expect(segs.length).toBeGreaterThan(0);
    segs.forEach((s) => {
      expect(s.kind).toBe("planetOrbit");
      expect(s.bodyName).toBeTruthy();
      expect(s.bodyName).not.toBe(ctx.rootBody);
      expect(ctx.hierarchy.planetNames).toContain(s.bodyName!);
      expect(isMoonBody(ctx, s.bodyName!)).toBe(false);
      expect(s.points.length).toBeGreaterThanOrEqual(512);
      expect(s.closed).toBe(true);
      expect(s.points.length).not.toBe(48);
      if (s.referenceBody) {
        expect(s.referenceBody).toBe(ctx.rootBody);
      }
      // Samples-first (v2 policy): fixture has sample points → UT for prograde direction
      if (s.bodyName === "Kerbin") {
        expect(s.sampleUniversalTimes?.length).toBeGreaterThanOrEqual(2);
      }
    });
  });
});
