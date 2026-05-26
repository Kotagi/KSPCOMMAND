import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-moons.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildSegments as buildV2BodyOrbitSegments } from "../../../map-v2/TrajectoryPlanner";
import { buildMapContext, isMoonBody } from "../../MapContext";
import { shouldIncludeMoonOrbit } from "./filterMoonOrbit";

const snap = fixture as TelemetrySnapshot;

describe("shouldIncludeMoonOrbit", () => {
  it("includes Mun and Minmus, excludes heliocentric Kerbin", () => {
    const ctx = buildMapContext(snap)!;
    const paths = ctx.telemetry.bodyOrbitPaths ?? [];

    const included = paths.filter((p) => shouldIncludeMoonOrbit(ctx, p));
    const names = included.map((p) => p.bodyName).sort();
    expect(names).toEqual(["Minmus", "Mun"]);

    const kerbin = paths.find((p) => p.bodyName === "Kerbin");
    expect(kerbin).toBeDefined();
    expect(shouldIncludeMoonOrbit(ctx, kerbin!)).toBe(false);
  });

  it("matches v2 BodyOrbit moonOnly body names on fixture", () => {
    const ctx = buildMapContext(snap)!;
    const paths = ctx.telemetry.bodyOrbitPaths ?? [];

    const v3Included = paths.filter((p) => shouldIncludeMoonOrbit(ctx, p));
    const v2Segs = buildV2BodyOrbitSegments(ctx, "BodyOrbit", {
      moonOnly: true,
    });

    const v3Names = new Set(v3Included.map((p) => p.bodyName));
    const v2Names = new Set(v2Segs.map((s) => s.bodyName));
    expect(v3Names).toEqual(v2Names);
    v3Included.forEach((p) => {
      expect(isMoonBody(ctx, p.bodyName!)).toBe(true);
    });
  });
});
