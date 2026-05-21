import { describe, expect, it } from "vitest";
import {
  ANALYTIC_DISPLAY_THRESHOLD_METERS,
  buildBodyOrbitTrailSegments,
  canUseAnalyticBodyOrbit,
  findBodyOrbitAnchor,
  resolveTrailRenderMode,
} from "./buildBodyOrbitTrail";
import type { BodyOrbitPath } from "../telemetry/schema-v6";

describe("buildBodyOrbitTrail", () => {
  const kerbinOrbit: BodyOrbitPath = {
    bodyName: "Kerbin",
    referenceBody: "Sun",
    classification: "Elliptic",
    orbitElements: {
      referenceBody: "Sun",
      classification: "Elliptic",
      referenceBodyRadiusMeters: 600000,
      semiMajorAxisMeters: 1.2e11,
      semiLatusRectumMeters: 1.2e11,
      eccentricity: 0.05,
      inclinationDegrees: 0,
      longitudeOfAscendingNodeDegrees: 0,
      argumentOfPeriapsisDegrees: 0,
      epochUniversalTimeSeconds: 0,
      periodSeconds: 9e6,
    },
  };

  it("detects analytic-capable paths", () => {
    expect(canUseAnalyticBodyOrbit(kerbinOrbit)).toBe(true);
    expect(canUseAnalyticBodyOrbit({ bodyName: "X" })).toBe(false);
  });

  it("builds closed elliptic segments around anchor", () => {
    const anchor = { x: 1e11, y: 0, z: 0 };
    const segments = buildBodyOrbitTrailSegments(kerbinOrbit, anchor);
    expect(segments.length).toBeGreaterThan(0);
    const points = segments[0];
    expect(points.length).toBeGreaterThan(32);
    const first = points[0];
    const last = points[points.length - 1];
    const dx = first.x - last.x;
    const dy = first.y - last.y;
    const dz = first.z - last.z;
    expect(dx * dx + dy * dy + dz * dz).toBeLessThan(1e20);
    points.forEach((p) => {
      const rx = p.x - anchor.x;
      const ry = p.y - anchor.y;
      const rz = p.z - anchor.z;
      const r = Math.sqrt(rx * rx + ry * ry + rz * rz);
      expect(r).toBeGreaterThan(5e10);
      expect(r).toBeLessThan(2e11);
    });
  });

  it("honors telemetry trailRenderMode hidden", () => {
    const hidden = {
      ...kerbinOrbit,
      validation: { trailRenderMode: "hidden" as const },
    };
    expect(resolveTrailRenderMode(hidden)).toBe("hidden");
  });

  it("shows samples when telemetry hidden but icon matches sample 0", () => {
    const hiddenButAligned = {
      ...kerbinOrbit,
      samples: kerbinOrbit.samples ?? [
        {
          sampleUniversalTimeSeconds: 0,
          positionRootRelativeMeters: { x: 1.35e11, y: 0, z: 0 },
        },
        {
          sampleUniversalTimeSeconds: 1,
          positionRootRelativeMeters: { x: 1.34e11, y: 1e9, z: 0 },
        },
      ],
      validation: {
        trailRenderMode: "hidden" as const,
        liveToSample0Meters: 0,
        maxSampleToTrailFrameMeters: 0,
        periodClosureMeters: 1e9,
      },
    };
    expect(resolveTrailRenderMode(hiddenButAligned)).toBe("samples");
  });

  it("prefers analytic when live residual below threshold", () => {
    const analytic = {
      ...kerbinOrbit,
      validation: {
        liveToAnalyticMeters: ANALYTIC_DISPLAY_THRESHOLD_METERS * 0.5,
        maxSampleToRecomputedMeters: 1e9,
        periodClosureMeters: 1e9,
      },
    };
    expect(resolveTrailRenderMode(analytic)).toBe("analytic");
  });

  it("uses parent position from sample when provided", () => {
    const anchor = findBodyOrbitAnchor(
      { bodyName: "Mun", referenceBody: "Kerbin", classification: "Elliptic" },
      [],
      "Sun",
      { parentPositionRootRelativeMeters: { x: 9e10, y: 0, z: 0 } },
    );
    expect(anchor).toEqual({ x: 9e10, y: 0, z: 0 });
  });

  it("findBodyOrbitAnchor uses body list or origin for root", () => {
    expect(
      findBodyOrbitAnchor(kerbinOrbit, [], "Sun"),
    ).toEqual({ x: 0, y: 0, z: 0 });
    expect(
      findBodyOrbitAnchor(kerbinOrbit, [
        {
          body: { name: "Sun" },
          position: { x: 0, y: 0, z: 0 },
        },
      ], "Sun"),
    ).toEqual({ x: 0, y: 0, z: 0 });
  });
});
