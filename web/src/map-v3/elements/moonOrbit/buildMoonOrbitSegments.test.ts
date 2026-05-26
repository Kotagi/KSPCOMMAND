import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-moons.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildMapContext, isMoonBody, isPlanetBody } from "../../MapContext";
import { buildMoonOrbitSegments } from "./buildMoonOrbitSegments";
import { buildPlanetOrbitSegments } from "../planetOrbit/buildPlanetOrbitSegments";

const snap = fixture as TelemetrySnapshot;

describe("buildMoonOrbitSegments", () => {
  it("builds moon trails only with parent reference and 512 verts", () => {
    const ctx = buildMapContext(snap)!;
    const segs = buildMoonOrbitSegments(ctx);
    expect(segs.length).toBe(2);
    segs.forEach((s) => {
      expect(s.kind).toBe("moonOrbit");
      expect(s.bodyName).toBeTruthy();
      expect(isMoonBody(ctx, s.bodyName!)).toBe(true);
      expect(isPlanetBody(ctx, s.bodyName!)).toBe(false);
      expect(s.parentBody).toBe("Kerbin");
      expect(s.referenceBody).toBe("Kerbin");
      expect(s.referenceBody).not.toBe(ctx.rootBody);
      expect(s.points.length).toBeGreaterThanOrEqual(512);
      expect(s.closed).toBe(true);
    });
  });

  it("does not duplicate planet orbit segments", () => {
    const ctx = buildMapContext(snap)!;
    const moons = buildMoonOrbitSegments(ctx);
    const planets = buildPlanetOrbitSegments(ctx);
    const moonNames = new Set(moons.map((s) => s.bodyName));
    planets.forEach((p) => {
      expect(moonNames.has(p.bodyName)).toBe(false);
    });
  });
});
