import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-moons.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildMapContext } from "../../MapContext";
import { buildMoonOrbitSegments } from "./buildMoonOrbitSegments";
import {
  liveMoonParentRelativePosition,
  resolveMoonOrbitParentRelativePointsFromPath,
  samplePositionsParentRelativeFromPath,
  sampleRootPositionsFromMoonPath,
} from "./moonOrbitGeometry";

const snap = fixture as TelemetrySnapshot;

describe("moonOrbitGeometry", () => {
  it("uses per-sample parent-relative offsets for ring source", () => {
    const ctx = buildMapContext(snap)!;
    const munPath = ctx.telemetry.bodyOrbitPaths!.find(
      (p) => p.bodyName === "Mun",
    )!;
    const root = sampleRootPositionsFromMoonPath(munPath);
    const parentRelative = samplePositionsParentRelativeFromPath(munPath, ctx);
    expect(root.length).toBeGreaterThanOrEqual(2);
    const rootMax = Math.max(...root.map((p) => Math.hypot(p.x, p.y, p.z)));
    const relMax = Math.max(
      ...parentRelative.map((p) => Math.hypot(p.x, p.y, p.z)),
    );
    expect(rootMax).toBeGreaterThan(1e10);
    expect(relMax).toBeLessThan(2e7);
  });

  it("buildMoonOrbitSegments densifies sample polylines to 512 verts", () => {
    const ctx = buildMapContext(snap)!;
    const segs = buildMoonOrbitSegments(ctx);
    segs.forEach((s) => {
      expect(s.points.length).toBeGreaterThanOrEqual(512);
    });
    const mun = segs.find((s) => s.bodyName === "Mun")!;
    expect(mun.geometrySource).toBe("samples");
    expect(mun.sampleUniversalTimes?.length).toBeGreaterThanOrEqual(512);
  });

  it("anchor index targets live moon parent-relative position on the ring", () => {
    const ctx = buildMapContext(snap)!;
    const seg = buildMoonOrbitSegments(ctx).find((s) => s.bodyName === "Mun")!;
    const live = liveMoonParentRelativePosition(ctx, "Mun", "Kerbin")!;
    const anchor = seg.points[seg.anchorIndex ?? 0];
    const dist = Math.hypot(
      anchor.x - live.x,
      anchor.y - live.y,
      anchor.z - live.z,
    );
    expect(dist).toBeLessThan(2e6);
  });

  it("fills parent-relative from parent orbit path when sample omits parentPosition", () => {
    const ctx = buildMapContext(snap)!;
    const munPath = ctx.telemetry.bodyOrbitPaths!.find(
      (p) => p.bodyName === "Mun",
    )!;
    const sparseSamples = (munPath.samples ?? []).map((s, i) => ({
      ...s,
      parentPositionRootRelativeMeters:
        i % 32 === 0 ? s.parentPositionRootRelativeMeters : undefined,
    }));
    const sparsePath = { ...munPath, samples: sparseSamples };
    const withCtx = samplePositionsParentRelativeFromPath(sparsePath, ctx);
    const noCtx = samplePositionsParentRelativeFromPath(sparsePath, null);
    expect(withCtx.length).toBeGreaterThan(noCtx.length);
    expect(withCtx.length).toBe((munPath.samples ?? []).length);
  });

  it("resolveMoonOrbitParentRelativePointsFromPath uses telemetry samples first", () => {
    const ctx = buildMapContext(snap)!;
    const munPath = ctx.telemetry.bodyOrbitPaths!.find(
      (p) => p.bodyName === "Mun",
    )!;
    const resolved = resolveMoonOrbitParentRelativePointsFromPath(munPath, ctx);
    const raw = samplePositionsParentRelativeFromPath(munPath, ctx);
    expect(resolved.length).toBe(raw.length);
    expect(raw.length).toBeGreaterThanOrEqual(2);
  });
});
