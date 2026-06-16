import { describe, expect, it } from "vitest";
import fixture from "../../../../fixtures/telemetry-kerbin-orientation-v10.json";
import type { TelemetrySnapshot } from "../../../telemetry/schema-v6";
import {
  buildSpinChiralityReport,
  sampleFromKerbinBody,
  signedSpinAboutAxis,
  type KerbinChiralitySample,
} from "./planetBodySpinChiralityDiagnostic";
import { kspRootQuaternionObliquityTilt } from "../../../coords/kspBodyOrientation";
import type { CelestialBodyWithOrientation } from "./planetBodyOrientationFields";

const snap = fixture as TelemetrySnapshot;
const kerbin = snap.bodies!.find((b) => b.name === "Kerbin")! as CelestialBodyWithOrientation;

describe("planetBodySpinChiralityDiagnostic", () => {
  it("Kerbin fixture sample has orientation", () => {
    const sample = sampleFromKerbinBody(kerbin, snap.gameUniversalTimeSeconds!);
    expect(sample?.bodyOrientationRootRelative.w).toBeGreaterThan(0);
  });

  it("signed spin about north matches increasing rotationAngle on synthetic pair", () => {
    const q0 = kspRootQuaternionObliquityTilt(Math.PI / 8, 1);
    const q1 = kspRootQuaternionObliquityTilt(Math.PI / 8, 1.2);
    const north = { x: 0, y: 1, z: 0 };
    const sign = Math.sign(signedSpinAboutAxis(q0, q1, north));
    expect(sign).toBeGreaterThan(0);
  });

  it("buildSpinChiralityReport classifies mesh vs rotationAngle on stepped samples", () => {
    const earlier: KerbinChiralitySample = {
      gameUt: 1000,
      rotationAngleRadians: 1,
      bodyOrientationRootRelative: kspRootQuaternionObliquityTilt(
        Math.PI / 8,
        1,
      ),
      spinAxisRootRelative: { x: 0, y: 0, z: 1 },
    };
    const later: KerbinChiralitySample = {
      gameUt: 1100,
      rotationAngleRadians: 1.4,
      bodyOrientationRootRelative: kspRootQuaternionObliquityTilt(
        Math.PI / 8,
        1.4,
      ),
      spinAxisRootRelative: { x: 0, y: 0, z: 1 },
    };
    const report = buildSpinChiralityReport(earlier, later);
    expect(report.telemetryQuaternionAgreesRotationAngle).toBe(true);
    expect(report.signRotationAngle).toBeGreaterThan(0);
    expect(report.production.signEquatorCrossThree).toBeGreaterThan(0);
    expect(report.verdict).toBe("mesh-matches-ksp-rotation-angle");
    expect(report.userVerification.length).toBeGreaterThan(3);
  });
});
