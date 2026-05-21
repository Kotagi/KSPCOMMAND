import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-stable.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildMapContext } from "../../MapContext";
import { buildStarMarkerSegments } from "./buildStarMarkerSegments";
import { resolveSystemAnchors } from "./resolveSystemAnchors";

const snap = fixture as TelemetrySnapshot;

describe("buildStarMarkerSegments", () => {
  it("builds one segment at root for primary star", () => {
    const ctx = buildMapContext(snap);
    expect(ctx).not.toBeNull();
    const segs = buildStarMarkerSegments(ctx!);
    expect(segs.length).toBe(1);
    expect(segs[0].kind).toBe("starMarker");
    expect(segs[0].bodyName).toBe(ctx!.rootBody);
    const pos = segs[0].points[0];
    expect(Math.hypot(pos.x, pos.y, pos.z)).toBeLessThan(1e6);
  });

  it("resolveSystemAnchors returns one anchor keyed by root body", () => {
    const ctx = buildMapContext(snap)!;
    const anchors = resolveSystemAnchors(ctx);
    expect(anchors.length).toBe(1);
    expect(anchors[0].bodyName).toBe(ctx.rootBody);
    expect(anchors[0].radiusMeters).toBeGreaterThan(0);
  });
});
