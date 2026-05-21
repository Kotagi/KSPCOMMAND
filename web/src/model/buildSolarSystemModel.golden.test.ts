import { describe, expect, it } from "vitest";
import fixture from "../../fixtures/telemetry-kerbin-stable.json";
import type { TelemetrySnapshot } from "../telemetry/schema-v6";
import { buildSolarSystemModel } from "./buildSolarSystemModel";
import { resolveTrailRenderMode } from "../coords/buildBodyOrbitTrail";

describe("telemetry-kerbin-stable golden", () => {
  const telemetry = fixture as TelemetrySnapshot;

  it("builds model with Kerbin on expected position", () => {
    const model = buildSolarSystemModel(telemetry, {
      scrubEnabled: false,
      scrubUniversalTime: null,
    });
    const kerbin = model.bodies.find((b) => b.body.name === "Kerbin");
    expect(kerbin?.position.x).toBeCloseTo(1.35e11, -6);
  });

  it("validation passes analytic gate for Kerbin", () => {
    const path = telemetry.bodyOrbitPaths?.[0];
    expect(path).toBeDefined();
    expect(resolveTrailRenderMode(path!)).toBe("analytic");
    expect(path!.validation?.liveToSample0Meters).toBeLessThanOrEqual(1);
    expect(path!.validation?.liveToAnalyticMeters).toBeLessThanOrEqual(5e4);
  });
});
