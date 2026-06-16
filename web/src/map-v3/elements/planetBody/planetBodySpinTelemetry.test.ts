import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-orientation-v10.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import {
  describeSiderealSpinAlignment,
  resolveSiderealSpinAngleRadians,
  resolveSiderealSpinRateRadPerSec,
  resolveStockSiderealSpinRateRadPerSec,
  rotateBodyFixedPointAtUt,
  type CelestialBodyWithOrientation,
} from "./planetBodyOrientationFields";

const snap = fixture as TelemetrySnapshot;

function kerbinFromFixture(): CelestialBodyWithOrientation {
  const body = snap.bodies?.find((b) => b.name === "Kerbin");
  if (!body) {
    throw new Error("Kerbin missing from fixture");
  }
  return body as CelestialBodyWithOrientation;
}

describe("planetBodySpinTelemetry (Kerbin v10 fixture)", () => {
  const kerbin = kerbinFromFixture();
  const KERBIN_PERIOD = kerbin.rotationPeriodSeconds!;
  const STOCK_RATE = (2 * Math.PI) / KERBIN_PERIOD;

  it("uses stock rotation period rate for Kerbin", () => {
    expect(resolveStockSiderealSpinRateRadPerSec(kerbin)).toBeCloseTo(
      STOCK_RATE,
      8,
    );
    expect(resolveSiderealSpinRateRadPerSec(kerbin)).toBeCloseTo(STOCK_RATE, 8);
  });

  it("live Kerbin: ω·n̂ opposes period sign (ω on -Z, n̂ on +Z)", () => {
    const diag = describeSiderealSpinAlignment(kerbin);
    expect(diag.omegaAgreesWithStock).toBe(false);
    expect(diag.omegaDotSpinAxis).toBeLessThan(-0.99);
  });

  it("stock rate unchanged when ω aligns with n̂ (synthetic)", () => {
    const aligned = {
      ...kerbin,
      angularVelocityRootRelativeRadPerSec: {
        x: 0,
        y: 0,
        z: STOCK_RATE,
      },
    };
    expect(resolveSiderealSpinRateRadPerSec(aligned)).toBeCloseTo(STOCK_RATE, 8);
    expect(describeSiderealSpinAlignment(aligned).omegaAgreesWithStock).toBe(
      true,
    );
  });

  it("returns to same attitude after one Kerbin sidereal period (scrub)", () => {
    const sampleUt = kerbin.bodyOrientationSampleUniversalTimeSeconds!;
    const angle = resolveSiderealSpinAngleRadians(kerbin, KERBIN_PERIOD);
    expect(angle).toBeCloseTo(2 * Math.PI, 5);

    const p0 = rotateBodyFixedPointAtUt(
      kerbin,
      { x: 1, y: 0, z: 0 },
      sampleUt,
    )!;
    const p1 = rotateBodyFixedPointAtUt(
      kerbin,
      { x: 1, y: 0, z: 0 },
      sampleUt + KERBIN_PERIOD,
    )!;
    expect(p0.x).toBeCloseTo(p1.x, 3);
    expect(p0.y).toBeCloseTo(p1.y, 3);
    expect(p0.z).toBeCloseTo(p1.z, 3);
  });

  it("moves equator marker prograde over +6h (half period, scrub)", () => {
    const sampleUt = kerbin.bodyOrientationSampleUniversalTimeSeconds!;
    const p0 = rotateBodyFixedPointAtUt(
      kerbin,
      { x: 1, y: 0, z: 0 },
      sampleUt,
    )!;
    const p1 = rotateBodyFixedPointAtUt(
      kerbin,
      { x: 1, y: 0, z: 0 },
      sampleUt + KERBIN_PERIOD / 2,
    )!;
    const cross =
      p0.x * p1.y - p0.y * p1.x + p0.y * p1.z - p0.z * p1.y + p0.z * p1.x - p0.x * p1.z;
    const north = kerbin.spinAxisRootRelative!;
    const expected =
      north.x * (p1.y * p0.z - p1.z * p0.y) +
      north.y * (p1.z * p0.x - p1.x * p0.z) +
      north.z * (p1.x * p0.y - p1.y * p0.x);
    expect(Math.sign(cross)).toBe(Math.sign(expected));
  });
});
