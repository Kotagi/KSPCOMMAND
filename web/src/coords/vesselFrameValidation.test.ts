import { describe, expect, it } from "vitest";
import type { TelemetrySnapshot } from "../telemetry/schema-v6";
import { validateVesselFrameAlignment } from "./vesselFrameValidation";

function coplanarOrbit(
  center: { x: number; y: number; z: number },
  radius: number,
  count: number,
): { x: number; y: number; z: number }[] {
  return Array.from({ length: count }, (_, i) => {
    const t = (i / (count - 1)) * Math.PI * 0.5;
    return {
      x: center.x + radius * Math.cos(t),
      y: center.y + radius * Math.sin(t),
      z: center.z,
    };
  });
}

describe("validateVesselFrameAlignment", () => {
  it("passes when vessel path shares XY plane with reference body trail", () => {
    const kerbinTrail = coplanarOrbit({ x: 1.35e11, y: 0, z: 0 }, 2e9, 8);
    const vesselTrail = coplanarOrbit({ x: 1.36e11, y: 1e8, z: 0 }, 5e8, 8);
    const telemetry = {
      schemaVersion: 8,
      orbit: { referenceBody: "Kerbin" },
      activeVessel: {
        name: "Test",
        positionRootRelativeMeters: vesselTrail[0],
        rootPathSamples: vesselTrail.map((p, i) => ({
          sampleUniversalTimeSeconds: 100000 + i,
          positionRootRelativeMeters: p,
        })),
      },
      bodyOrbitPaths: [
        {
          bodyName: "Kerbin",
          samples: kerbinTrail.map((p, i) => ({
            sampleUniversalTimeSeconds: 100000 + i,
            positionRootRelativeMeters: p,
          })),
        },
      ],
    } as TelemetrySnapshot;

    const result = validateVesselFrameAlignment(telemetry);
    expect(result.ok).toBe(true);
    expect(result.liveToPath0Meters).toBe(0);
    expect(result.planeAngleDegrees).not.toBeNull();
    expect(result.planeAngleDegrees!).toBeLessThan(1);
  });

  it("fails when vessel path is tilted ~90° from reference trail", () => {
    const kerbinTrail = coplanarOrbit({ x: 1.35e11, y: 0, z: 0 }, 2e9, 8);
    const vesselTrail = coplanarOrbit({ x: 1.36e11, y: 0, z: 0 }, 5e8, 8).map(
      (p) => ({ x: p.x, y: 0, z: p.y - 1.36e11 }),
    );
    const telemetry = {
      schemaVersion: 8,
      orbit: { referenceBody: "Kerbin" },
      activeVessel: {
        name: "Test",
        positionRootRelativeMeters: vesselTrail[0],
        rootPathSamples: vesselTrail.map((p, i) => ({
          sampleUniversalTimeSeconds: 100000 + i,
          positionRootRelativeMeters: p,
        })),
      },
      bodyOrbitPaths: [
        {
          bodyName: "Kerbin",
          samples: kerbinTrail.map((p, i) => ({
            sampleUniversalTimeSeconds: 100000 + i,
            positionRootRelativeMeters: p,
          })),
        },
      ],
    } as TelemetrySnapshot;

    const result = validateVesselFrameAlignment(telemetry);
    expect(result.ok).toBe(false);
    expect(result.planeAngleDegrees).not.toBeNull();
    expect(result.planeAngleDegrees!).toBeGreaterThan(80);
  });
});
