import { describe, expect, it } from "vitest";
import * as THREE from "three";
import fixture from "../../../../fixtures/telemetry-kerbin-orientation-v10.json";
import { BODY_TEXTURE_MIRROR_U } from "../../../assets/planetBodyTextures";
import { kspRootQuaternionToThree } from "../../../coords/kspBodyOrientation";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import {
  resolvePlanetBodyOrientationAtUt,
  resolveSiderealSpinRateRadPerSec,
  resolveStockSiderealSpinRateRadPerSec,
} from "./planetBodyOrientationFields";

const snap = fixture as TelemetrySnapshot;
const kerbin = snap.bodies!.find((b) => b.name === "Kerbin")!;

describe("planetBodyDisplaySpin", () => {
  it("does not mirror U on the web (longitude from DLL export flip-X)", () => {
    expect(BODY_TEXTURE_MIRROR_U).toBe(false);
  });

  it("uses stock Kerbin period rate", () => {
    const stock = resolveStockSiderealSpinRateRadPerSec(kerbin);
    expect(stock).toBeCloseTo(
      (2 * Math.PI) / (kerbin.rotationPeriodSeconds ?? 21600),
      6,
    );
    expect(resolveSiderealSpinRateRadPerSec(kerbin)).toBeCloseTo(stock!, 8);
  });

  it("Kerbin +6h extrapolation moves marker in Three (prograde)", () => {
    const sampleUt = kerbin.bodyOrientationSampleUniversalTimeSeconds!;
    const dt = 21600 / 2;
    const q0 = resolvePlanetBodyOrientationAtUt(kerbin, sampleUt)!;
    const q1 = resolvePlanetBodyOrientationAtUt(kerbin, sampleUt + dt)!;
    const marker = new THREE.Vector3(1, 0, 0);
    const t0 = marker.clone().applyQuaternion(kspRootQuaternionToThree(q0));
    const t1 = marker.clone().applyQuaternion(kspRootQuaternionToThree(q1));
    expect(t0.length()).toBeCloseTo(1, 5);
    expect(t1.length()).toBeCloseTo(1, 5);
    expect(t0.angleTo(t1)).toBeGreaterThan(0.05);
  });
});
