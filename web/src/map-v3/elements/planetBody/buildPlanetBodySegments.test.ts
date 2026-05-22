import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-stable.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildSegments as buildV2Segments } from "../../../map-v2/TrajectoryPlanner";
import { buildMapContext } from "../../MapContext";
import { buildPlanetBodySegments } from "./buildPlanetBodySegments";

const snap = fixture as TelemetrySnapshot;

describe("buildPlanetBodySegments", () => {
  it("emits one segment per planet with position from bodyByName", () => {
    const ctx = buildMapContext(snap)!;
    const segs = buildPlanetBodySegments(ctx);
    expect(segs.length).toBeGreaterThan(0);
    segs.forEach((seg) => {
      expect(seg.kind).toBe("planetBody");
      expect(seg.points).toHaveLength(1);
      expect(seg.bodyName).toBeTruthy();
      expect(ctx.hierarchy.planetNames).toContain(seg.bodyName!);
      expect(seg.bodyName).not.toBe(ctx.rootBody);
      const entry = ctx.bodyByName.get(seg.bodyName!);
      expect(seg.points[0]).toEqual(entry!.position);
    });
  });

  it("matches v2 BodyPosition planetOnly body names on fixture", () => {
    const ctx = buildMapContext(snap)!;
    const v3 = new Set(buildPlanetBodySegments(ctx).map((s) => s.bodyName));
    const v2 = new Set(
      buildV2Segments(ctx, "BodyPosition", { planetOnly: true }).map(
        (s) => s.bodyName,
      ),
    );
    expect(v3).toEqual(v2);
  });
});
