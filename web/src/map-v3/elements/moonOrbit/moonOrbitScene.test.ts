import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-moons.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildMapContext } from "../../MapContext";
import { createSceneFrame } from "../../SceneFrame";
import { buildMoonOrbitSegments } from "./buildMoonOrbitSegments";
import { liveMoonParentRelativePosition } from "./moonOrbitGeometry";
import { moonOrbitRingPlaneNormal } from "./moonOrbitPlacement";
import {
  moonOrbitPointsToScene,
  resolveMoonOrbitAnchorIndex,
} from "./moonOrbitScene";
import { toScenePoint } from "../../SceneFrame";

const snap = fixture as TelemetrySnapshot;

describe("moonOrbitScene", () => {
  it("equatorial Mun ring contains live moon offset in plane", () => {
    const ctx = buildMapContext(snap)!;
    const kerbin = ctx.bodyByName.get("Kerbin")!;
    const seg = buildMoonOrbitSegments(ctx).find((s) => s.bodyName === "Mun")!;
    const frame = createSceneFrame(ctx, 1e-9, "body", "Kerbin");
    frame.focus = kerbin.position;

    expect(seg.geometrySource).toBe("samples");
    const scene = moonOrbitPointsToScene(seg.points, kerbin, frame);
    expect(scene.length).toBeGreaterThanOrEqual(512);

    const normal = moonOrbitRingPlaneNormal(seg.points);
    expect(normal).not.toBeNull();

    const liveRel = liveMoonParentRelativePosition(ctx, "Mun", "Kerbin")!;
    const liveLen = Math.hypot(liveRel.x, liveRel.y, liveRel.z);
    const liveInPlane =
      Math.abs(
        (normal!.x * liveRel.x + normal!.y * liveRel.y + normal!.z * liveRel.z)
          / liveLen,
      );
    expect(liveInPlane).toBeLessThan(0.08);
  });

  it("anchors motion tail on scene ring near moon icon position", () => {
    const ctx = buildMapContext(snap)!;
    const kerbin = ctx.bodyByName.get("Kerbin")!;
    const mun = ctx.bodyByName.get("Mun")!;
    const seg = buildMoonOrbitSegments(ctx).find((s) => s.bodyName === "Mun")!;
    const frame = createSceneFrame(ctx, 1e-9, "body", "Kerbin");
    frame.focus = kerbin.position;

    const scene = moonOrbitPointsToScene(seg.points, kerbin, frame);
    const anchorIndex = resolveMoonOrbitAnchorIndex(scene, "Mun", ctx, frame);
    const moonScene = toScenePoint(mun.position, frame);
    const anchor = scene[anchorIndex]!;

    const dist = Math.hypot(
      anchor[0] - moonScene[0],
      anchor[1] - moonScene[1],
      anchor[2] - moonScene[2],
    );
    const orbitRadiusScene = Math.hypot(
      liveMoonParentRelativePosition(ctx, "Mun", "Kerbin")!.x,
      liveMoonParentRelativePosition(ctx, "Mun", "Kerbin")!.y,
      liveMoonParentRelativePosition(ctx, "Mun", "Kerbin")!.z,
    ) * 1e-9;
    expect(dist).toBeLessThan(orbitRadiusScene * 0.05);
  });
});
