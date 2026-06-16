import { describe, expect, it } from "vitest";
import {
  isPlanetBodyOrientationReady,
  readPlanetBodyOrientation,
  resolveSiderealSpinAxisRootRelative,
  resolveSiderealSpinRateRadPerSec,
  rotateBodyFixedPointAtUt,
} from "./planetBodyOrientationFields";

describe("planetBodyOrientationFields", () => {
  it("requires bodyOrientationRootRelative", () => {
    expect(isPlanetBodyOrientationReady(undefined)).toBe(false);
    expect(isPlanetBodyOrientationReady({})).toBe(false);
    expect(
      isPlanetBodyOrientationReady({
        bodyOrientationRootRelative: { x: 0, y: 0, z: 0, w: 1 },
      }),
    ).toBe(true);
  });

  it("normalizes on read", () => {
    const q = readPlanetBodyOrientation({
      name: "Kerbin",
      bodyOrientationRootRelative: { x: 0, y: 0, z: 0, w: 2 },
    });
    expect(q?.w).toBeCloseTo(1, 6);
  });

  it("prefers angular velocity for spin axis", () => {
    const body = {
      name: "Kerbin",
      bodyOrientationRootRelative: { x: 0, y: 0, z: 0, w: 1 },
      angularVelocityRootRelativeRadPerSec: { x: 0, y: 0, z: 2 },
      spinAxisRootRelative: { x: 1, y: 0, z: 0 },
    };
    const axis = resolveSiderealSpinAxisRootRelative(body);
    expect(axis?.z).toBeCloseTo(1, 6);
  });

  it("uses stock rotationPeriod / inverseRotation for rate", () => {
    const period = 100;
    const magnitude = (2 * Math.PI) / period;
    const body = {
      name: "Eeloo",
      rotates: true,
      inverseRotation: true,
      rotationPeriodSeconds: period,
      angularVelocityRootRelativeRadPerSec: { x: 0, y: 0, z: -1 },
      spinAxisRootRelative: { x: 0, y: 0, z: 1 },
    };
    expect(resolveSiderealSpinRateRadPerSec(body)).toBeCloseTo(-magnitude, 8);
    const aligned = {
      ...body,
      inverseRotation: false,
      angularVelocityRootRelativeRadPerSec: { x: 0, y: 0, z: 1 },
    };
    expect(resolveSiderealSpinRateRadPerSec(aligned)).toBeCloseTo(magnitude, 8);
  });

  it("extrapolates inertial spin about north by UT delta", () => {
    const period = 4 * Math.PI;
    const body = {
      name: "Kerbin",
      rotates: true,
      rotationPeriodSeconds: period,
      inverseRotation: false,
      bodyOrientationSampleUniversalTimeSeconds: 100,
      bodyOrientationRootRelative: { x: 0, y: 0, z: 0, w: 1 },
      angularVelocityRootRelativeRadPerSec: { x: 0, y: 0.5, z: 0 },
      spinAxisRootRelative: { x: 0, y: 1, z: 0 },
    };
    const p0 = rotateBodyFixedPointAtUt(body, { x: 1, y: 0, z: 0 }, 100);
    const p1 = rotateBodyFixedPointAtUt(body, { x: 1, y: 0, z: 0 }, 100 + Math.PI);
    expect(p0?.x).toBeCloseTo(1, 5);
    expect(p1?.x).toBeCloseTo(0, 4);
    expect(p1?.z).toBeCloseTo(-1, 4);
  });

  it("moves a body-fixed equator point prograde about north", () => {
    const period = 4 * Math.PI;
    const body = {
      name: "Kerbin",
      rotates: true,
      inverseRotation: false,
      rotationPeriodSeconds: period,
      bodyOrientationSampleUniversalTimeSeconds: 0,
      bodyOrientationRootRelative: { x: 0, y: 0, z: 0, w: 1 },
      angularVelocityRootRelativeRadPerSec: { x: 0, y: 0.5, z: 0 },
      spinAxisRootRelative: { x: 0, y: 1, z: 0 },
    };
    const p0 = rotateBodyFixedPointAtUt(body, { x: 1, y: 0, z: 0 }, 0);
    const p1 = rotateBodyFixedPointAtUt(body, { x: 1, y: 0, z: 0 }, Math.PI);
    expect(p0?.x).toBeCloseTo(1, 5);
    expect(p0?.y).toBeCloseTo(0, 5);
    expect(p1?.x).toBeCloseTo(0, 4);
    expect(p1?.z).toBeCloseTo(-1, 4);
  });

  it("reverses equator motion when inverseRotation is true", () => {
    const period = 4 * Math.PI;
    const body = {
      name: "Eeloo",
      rotates: true,
      inverseRotation: true,
      rotationPeriodSeconds: period,
      bodyOrientationSampleUniversalTimeSeconds: 0,
      bodyOrientationRootRelative: { x: 0, y: 0, z: 0, w: 1 },
      angularVelocityRootRelativeRadPerSec: { x: 0, y: -0.5, z: 0 },
      spinAxisRootRelative: { x: 0, y: 1, z: 0 },
    };
    const p1 = rotateBodyFixedPointAtUt(body, { x: 1, y: 0, z: 0 }, Math.PI);
    expect(p1?.x).toBeCloseTo(0, 4);
    expect(p1?.z).toBeCloseTo(1, 4);
  });
});
