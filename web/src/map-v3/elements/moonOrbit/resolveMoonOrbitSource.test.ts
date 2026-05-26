import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-moons.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { PLANET_ORBIT_STYLE } from "../planetOrbit/planetOrbitStyle";
import { buildMapContext } from "../../MapContext";
import { resolveMoonOrbitSourcePoints } from "./resolveMoonOrbitSource";

const snap = fixture as TelemetrySnapshot;

describe("resolveMoonOrbitSourcePoints", () => {
  it("prefers telemetry samples over analytic for Mun fixture", () => {
    const ctx = buildMapContext(snap)!;
    const munPath = ctx.telemetry.bodyOrbitPaths!.find(
      (p) => p.bodyName === "Mun",
    )!;
    const { points, source } = resolveMoonOrbitSourcePoints(munPath, ctx);
    expect(source).toBe("samples");
    expect(points.length).toBeGreaterThanOrEqual(2);
    expect(points.length).toBeLessThan(PLANET_ORBIT_STYLE.trailVertices);
  });
});
