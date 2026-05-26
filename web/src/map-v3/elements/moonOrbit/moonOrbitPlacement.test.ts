import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-moons.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import { buildMapContext } from "../../MapContext";
import { resolveMoonOrbitSourcePoints } from "./resolveMoonOrbitSource";
import { parentRelativeOffsetsAnchoredToParentNow } from "./moonOrbitPlacement";

const snap = fixture as TelemetrySnapshot;

describe("moonOrbitPlacement", () => {
  it("anchors parent-relative ring to parent now in solar root", () => {
    const ctx = buildMapContext(snap)!;
    const kerbin = ctx.bodyByName.get("Kerbin")!;
    const mun = ctx.bodyByName.get("Mun")!;
    const munPath = ctx.telemetry.bodyOrbitPaths!.find(
      (p) => p.bodyName === "Mun",
    )!;
    const { points: parentRel } = resolveMoonOrbitSourcePoints(munPath, ctx);
    const solar = parentRelativeOffsetsAnchoredToParentNow(parentRel, kerbin);

    let best = Infinity;
    solar.forEach((p) => {
      const d = Math.hypot(
        p.x - mun.position.x,
        p.y - mun.position.y,
        p.z - mun.position.z,
      );
      if (d < best) {
        best = d;
      }
    });
    const liveLen = Math.hypot(
      mun.position.x - kerbin.position.x,
      mun.position.y - kerbin.position.y,
      mun.position.z - kerbin.position.z,
    );
    expect(best).toBeLessThan(liveLen * 0.12);
  });
});
