import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-stable.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildSegments as buildV2BodyOrbitSegments } from "../../../map-v2/TrajectoryPlanner";
import { buildMapContext, isPlanetBody } from "../../MapContext";
import { buildPlanetOrbitSegments } from "./buildPlanetOrbitSegments";
import { shouldIncludeHeliocentricPlanetOrbit } from "./filterHeliocentricPlanetOrbit";

const snap = fixture as TelemetrySnapshot;

describe("filterHeliocentricPlanetOrbit", () => {
  it("matches v2 BodyOrbit planetOnly path set on fixture", () => {
    const ctx = buildMapContext(snap)!;
    const paths = ctx.telemetry.bodyOrbitPaths ?? [];

    const v3Included = paths.filter((p) =>
      shouldIncludeHeliocentricPlanetOrbit(ctx, p),
    );
    const v2Segs = buildV2BodyOrbitSegments(ctx, "BodyOrbit", {
      planetOnly: true,
    }).filter((s) => {
      const name = s.bodyName;
      return (
        !!name &&
        name !== ctx.rootBody &&
        isPlanetBody(ctx, name)
      );
    });

    const v3Names = new Set(v3Included.map((p) => p.bodyName).sort());
    const v2Names = new Set(v2Segs.map((s) => s.bodyName).sort());
    expect(v3Names).toEqual(v2Names);
  });
});

describe("buildPlanetOrbitSegments v3-native", () => {
  it("produces same planet body names as v2 BodyOrbit planetOnly", () => {
    const ctx = buildMapContext(snap)!;
    const v3 = buildPlanetOrbitSegments(ctx);
    const v2 = buildV2BodyOrbitSegments(ctx, "BodyOrbit", { planetOnly: true })
      .filter((s) => s.bodyName && isPlanetBody(ctx, s.bodyName))
      .map((s) => s.bodyName!);

    expect(new Set(v3.map((s) => s.bodyName).sort())).toEqual(
      new Set(v2.sort()),
    );
  });
});
