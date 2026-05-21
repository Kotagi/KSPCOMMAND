import { describe, expect, it } from "vitest";
import { isRenderableVesselRootPath } from "../scene/vesselPathValidation";
import { repairVesselRootPathSamples } from "./resolveVesselPathPoints";
import type { TelemetrySnapshot, VesselRootPathSample } from "../telemetry/schema-v6";

const wedgeTelemetry: TelemetrySnapshot = {
  schemaVersion: 6,
  rootBody: "Sun",
  orbit: { referenceBody: "Kerbin" },
  orbitPatches: [{ isActivePatch: true, referenceBody: "Kerbin" }],
  bodyOrbitPaths: [
    {
      bodyName: "Kerbin",
      samples: [
        {
          sampleUniversalTimeSeconds: 64402676,
          positionRootRelativeMeters: { x: -2.29e9, y: 0, z: 1.34e10 },
        },
        {
          sampleUniversalTimeSeconds: 64404750,
          positionRootRelativeMeters: { x: -1.75e9, y: 1.13e5, z: 4.25e8 },
        },
        {
          sampleUniversalTimeSeconds: 64406824,
          positionRootRelativeMeters: { x: -1.2e9, y: 1.1e5, z: 8.5e8 },
        },
      ],
    },
  ],
};

const wedgeSamples: VesselRootPathSample[] = [
  {
    sampleUniversalTimeSeconds: 64402676,
    positionRootRelativeMeters: { x: -2.29e9, y: 0, z: 1.34e10 },
  },
  {
    sampleUniversalTimeSeconds: 64404750,
    positionRootRelativeMeters: { x: -2.48e7, y: 98022, z: 7.08e6 },
  },
  {
    sampleUniversalTimeSeconds: 64406824,
    positionRootRelativeMeters: { x: -6.52e7, y: 83390, z: 1.39e7 },
  },
];

describe("resolveVesselPathPoints", () => {
  it("repairs mixed-scale root path samples using reference-body ephemeris", () => {
    const raw = wedgeSamples.map((s) => s.positionRootRelativeMeters!);
    expect(isRenderableVesselRootPath(raw)).toBe(false);

    const repaired = repairVesselRootPathSamples(wedgeTelemetry, wedgeSamples);
    expect(repaired.length).toBe(3);
    expect(isRenderableVesselRootPath(repaired)).toBe(true);
    expect(
      Math.hypot(repaired[1].x, repaired[1].y, repaired[1].z),
    ).toBeGreaterThan(1e9);
  });
});
