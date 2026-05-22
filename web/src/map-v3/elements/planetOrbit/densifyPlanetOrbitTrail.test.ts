import { describe, expect, it } from "vitest";
import type { BodyOrbitPath } from "../../../telemetry/schema-v6";
import {
  planetOrbitTrailUsesAnalyticSource,
  resolvePlanetOrbitPointsFromPath,
} from "./densifyPlanetOrbitTrail";

const bodies = [{ body: { name: "Kerbin" }, position: { x: 10, y: 0, z: 0 } }];

function kerbinLikePath(overrides: Partial<BodyOrbitPath> = {}): BodyOrbitPath {
  return {
    bodyName: "Kerbin",
    referenceBody: "Sun",
    classification: "Elliptic",
    orbitElements: {
      referenceBody: "Sun",
      classification: "Elliptic",
      referenceBodyRadiusMeters: 600000,
      semiMajorAxisMeters: 1.35e11,
      semiLatusRectumMeters: 1.35e11,
      eccentricity: 0.05,
      inclinationDegrees: 0,
      longitudeOfAscendingNodeDegrees: 0,
      argumentOfPeriapsisDegrees: 0,
      epochUniversalTimeSeconds: 0,
      periodSeconds: 9e6,
      trueAnomalyDegreesAtCapture: 0,
      meanAnomalyRadiansAtCapture: 0,
    },
    samples: [
      {
        sampleUniversalTimeSeconds: 1000,
        positionRootRelativeMeters: { x: 100, y: 0, z: 0 },
      },
      {
        sampleUniversalTimeSeconds: 2000,
        positionRootRelativeMeters: { x: 0, y: 100, z: 0 },
      },
      {
        sampleUniversalTimeSeconds: 3000,
        positionRootRelativeMeters: { x: -100, y: 0, z: 0 },
      },
    ],
    validation: {
      liveToSample0Meters: 0,
      liveToAnalyticMeters: 5e8,
      maxSampleToRecomputedMeters: 1,
      maxSampleToTrailFrameMeters: 1,
      periodClosureMeters: 1000,
      trailRenderMode: "samples",
      trailWarning: null,
    },
    ...overrides,
  };
}

describe("densifyPlanetOrbitTrail source selection", () => {
  it("uses telemetry samples when trailRenderMode is samples", () => {
    const path = kerbinLikePath();
    expect(planetOrbitTrailUsesAnalyticSource(path)).toBe(false);
    const points = resolvePlanetOrbitPointsFromPath(path, bodies, "Sun", []);
    expect(points).toHaveLength(3);
    expect(points[0]).toEqual({ x: 100, y: 0, z: 0 });
    expect(points[1]).toEqual({ x: 0, y: 100, z: 0 });
  });

  it("falls back to analytic only when samples are missing", () => {
    const path = kerbinLikePath({
      samples: [],
      validation: {
        liveToSample0Meters: 0,
        liveToAnalyticMeters: 1000,
        maxSampleToRecomputedMeters: 0,
        maxSampleToTrailFrameMeters: 0,
        periodClosureMeters: 1000,
        trailRenderMode: "analytic",
        trailWarning: null,
      },
    });
    expect(planetOrbitTrailUsesAnalyticSource(path)).toBe(true);
    const points = resolvePlanetOrbitPointsFromPath(path, bodies, "Sun", []);
    expect(points.length).toBeGreaterThanOrEqual(3);
    expect(points[0].x).not.toBe(100);
  });
});
