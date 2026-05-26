import { describe, expect, it } from "vitest";
import {
  isPlanetBodyOrientationReady,
  readPlanetBodyOrientation,
  resolvePlanetBodyOrientationAtUt,
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

  it("reverses UT spin sign when inverseRotation is true", () => {
    const body = {
      name: "Eeloo",
      rotates: true,
      inverseRotation: true,
      bodyOrientationSampleUniversalTimeSeconds: 0,
      bodyOrientationRootRelative: { x: 0, y: 0, z: 0, w: 1 },
      angularVelocityRootRelativeRadPerSec: { x: 0, y: 0, z: 1 },
    };
    const forward = resolvePlanetBodyOrientationAtUt(body, Math.PI / 4);
    const bodyForward = { ...body, inverseRotation: false };
    const alsoForward = resolvePlanetBodyOrientationAtUt(bodyForward, Math.PI / 4);
    expect(forward?.z).toBeCloseTo(-(alsoForward?.z ?? 0), 5);
  });

  it("extrapolates spin by UT delta about north pole", () => {
    const body = {
      name: "Kerbin",
      rotates: true,
      bodyOrientationSampleUniversalTimeSeconds: 100,
      bodyOrientationRootRelative: { x: 0, y: 0, z: 0, w: 1 },
      angularVelocityRootRelativeRadPerSec: { x: 0, y: 0, z: 1 },
      spinAxisRootRelative: { x: 0, y: 0, z: 1 },
    };
    const atSample = resolvePlanetBodyOrientationAtUt(body, 100);
    const later = resolvePlanetBodyOrientationAtUt(body, 100 + Math.PI / 2);
    expect(atSample?.w).toBeCloseTo(1, 5);
    expect(later?.y).toBeCloseTo(Math.sin(Math.PI / 4), 4);
    expect(later?.w).toBeCloseTo(Math.cos(Math.PI / 4), 4);
  });
});
