import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-moons.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildMapContext, isMoonBody, isPlanetBody } from "../../MapContext";
import { buildMoonBodySegments } from "./buildMoonBodySegments";
import { buildPlanetBodySegments } from "../planetBody/buildPlanetBodySegments";

const snap = fixture as TelemetrySnapshot;

describe("buildMoonBodySegments", () => {
  it("builds one moon body segment per stock moon with parent reference", () => {
    const ctx = buildMapContext(snap)!;
    const segs = buildMoonBodySegments(ctx);
    expect(segs.length).toBe(2);
    segs.forEach((s) => {
      expect(s.kind).toBe("moonBody");
      expect(s.bodyName).toBeTruthy();
      expect(isMoonBody(ctx, s.bodyName!)).toBe(true);
      expect(isPlanetBody(ctx, s.bodyName!)).toBe(false);
      expect(s.parentBody).toBe("Kerbin");
      expect(s.points).toHaveLength(1);
    });
  });

  it("does not duplicate planet body segments", () => {
    const ctx = buildMapContext(snap)!;
    const moons = buildMoonBodySegments(ctx);
    const planets = buildPlanetBodySegments(ctx);
    const moonNames = new Set(moons.map((s) => s.bodyName));
    planets.forEach((p) => {
      expect(moonNames.has(p.bodyName)).toBe(false);
    });
  });
});
