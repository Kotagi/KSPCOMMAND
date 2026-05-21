import { describe, expect, it } from "vitest";
import {
  buildRouteChordSegments,
  isDegenerateRouteAnchor,
} from "./routeOverlay";
import type { RouteAnchor } from "../model/buildSolarSystemModel";

function anchor(
  patchIndex: number,
  role: string,
  position: { x: number; y: number; z: number },
  referenceBody = "Sun",
): RouteAnchor {
  return {
    patch: { patchIndex, referenceBody },
    projected: { x: 0, y: 0 },
    position,
    role,
  };
}

describe("routeOverlay", () => {
  it("treats Sun patch boundaries at the root origin as degenerate", () => {
    const sample = anchor(1, "patchStart", { x: 0, y: 0, z: 0 });
    expect(isDegenerateRouteAnchor(sample, "Sun")).toBe(true);
    expect(isDegenerateRouteAnchor(sample, "Kerbin")).toBe(false);
  });

  it("keeps encounter markers at the root", () => {
    const sample = anchor(1, "encounter", { x: 3e10, y: 0, z: 0 });
    expect(isDegenerateRouteAnchor(sample, "Sun")).toBe(false);
  });

  it("buildRouteChordSegments never emits a segment through the solar origin", () => {
    const anchors: RouteAnchor[] = [
      anchor(0, "patchStart", { x: 1.3e10, y: 0, z: 0 }, "Kerbin"),
      anchor(1, "patchStart", { x: 0, y: 0, z: 0 }, "Sun"),
      anchor(1, "encounter", { x: 3.3e10, y: 0, z: 1e9 }, "Sun"),
      anchor(1, "patchEnd", { x: 0, y: 0, z: 0 }, "Sun"),
    ];
    const segments = buildRouteChordSegments(anchors, "Sun");
    expect(segments.length).toBe(0);
    expect(
      segments.flat().some((p) => Math.hypot(p.x, p.y, p.z) < 1e7),
    ).toBe(false);
  });
});
