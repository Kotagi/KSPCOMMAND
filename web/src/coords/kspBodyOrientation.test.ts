import { describe, expect, it } from "vitest";
import * as THREE from "three";
import {
  angleBetweenDegrees,
  KSP_BODY_NORTH_LOCAL_THREE,
  kspMeshPoleOffsetQuaternion,
  kspNorthPoleRootDirection,
  kspRootAngularVelocityToThree,
  kspRootQuaternionFromAxisAngle,
  kspRootQuaternionObliquityTilt,
  kspRootQuaternionSpinAboutNorth,
  kspRootQuaternionToThree,
  kspRootVectorToThree,
  kspRootVectorToThreeTuple,
  kspWorldQuaternionToRootRelative,
  kspWorldVectorToRootRelative,
  rotateKspRootVector,
} from "./kspBodyOrientation";

describe("kspBodyOrientation", () => {
  it("maps world spin axis to root-relative orbit normal (zero tilt)", () => {
    const worldSpin = { x: 0, y: 1, z: 0 };
    const rootSpin = kspWorldVectorToRootRelative(worldSpin);
    expect(rootSpin.x).toBeCloseTo(0, 4);
    expect(rootSpin.y).toBeCloseTo(0, 4);
    expect(rootSpin.z).toBeCloseTo(1, 4);
    const rootQ = kspWorldQuaternionToRootRelative({ x: 0, y: 0, z: 0, w: 1 });
    const north = kspNorthPoleRootDirection(rootQ);
    expect(north.z).toBeCloseTo(1, 4);
    expect(Math.abs(north.x)).toBeLessThan(0.01);
    expect(Math.abs(north.y)).toBeLessThan(0.01);
    const northThree = kspRootVectorToThree(north);
    const spinThree = kspRootVectorToThree(rootSpin);
    expect(angleBetweenDegrees(northThree, spinThree)).toBeLessThan(0.1);
  });

  it("maps KSP position axes to Three (x, z, -y)", () => {
    expect(kspRootVectorToThreeTuple({ x: 1, y: 2, z: 3 })).toEqual([1, 3, -2]);
  });

  it("maps KSP +Y (north) to Three -Z", () => {
    const north = kspRootVectorToThree({ x: 0, y: 1, z: 0 });
    expect(north.x).toBeCloseTo(0, 6);
    expect(north.y).toBeCloseTo(0, 6);
    expect(north.z).toBeCloseTo(-1, 6);
  });

  it("maps KSP +Z to Three +Y", () => {
    const v = kspRootVectorToThree({ x: 0, y: 0, z: 1 });
    expect(v.x).toBeCloseTo(0, 6);
    expect(v.y).toBeCloseTo(1, 6);
    expect(v.z).toBeCloseTo(0, 6);
  });

  it("preserves identity quaternion under basis change", () => {
    const q = kspRootQuaternionToThree({ x: 0, y: 0, z: 0, w: 1 });
    expect(q.x).toBeCloseTo(0, 6);
    expect(q.y).toBeCloseTo(0, 6);
    expect(q.z).toBeCloseTo(0, 6);
    expect(q.w).toBeCloseTo(1, 6);
  });

  it("90° about KSP X tilts north pole toward Three +Y", () => {
    const q = kspRootQuaternionFromAxisAngle({ x: 1, y: 0, z: 0 }, Math.PI / 2);
    const north = rotateKspRootVector(q, { x: 0, y: 1, z: 0 });
    const northThree = kspRootVectorToThree(north);
    const target = new THREE.Vector3(0, 1, 0);
    expect(angleBetweenDegrees(northThree, target)).toBeLessThan(0.01);
  });

  it("obliquity preset separates tilt from spin", () => {
    const tiltOnly = kspRootQuaternionObliquityTilt(Math.PI / 4, 0);
    const north = rotateKspRootVector(tiltOnly, { x: 0, y: 1, z: 0 });
    expect(Math.abs(north.z)).toBeGreaterThan(0.5);
    expect(Math.abs(north.y)).toBeGreaterThan(0.5);

    const spinOnly = kspRootQuaternionObliquityTilt(0, Math.PI / 3);
    const prime = rotateKspRootVector(spinOnly, { x: 1, y: 0, z: 0 });
    expect(Math.abs(prime.x)).toBeCloseTo(0.5, 2);
    expect(Math.abs(prime.z)).toBeCloseTo(Math.sqrt(3) / 2, 2);
  });

  it("maps angular velocity axis consistently with vectors", () => {
    const av = kspRootAngularVelocityToThree({ x: 0, y: 1, z: 0 });
    expect(av.z).toBeCloseTo(-1, 6);
  });

  it("spin-about-north matches axis-angle helper", () => {
    const a = kspRootQuaternionSpinAboutNorth(1.234);
    const b = kspRootQuaternionFromAxisAngle({ x: 0, y: 1, z: 0 }, 1.234);
    expect(a.x).toBeCloseTo(b.x, 6);
    expect(a.y).toBeCloseTo(b.y, 6);
    expect(a.z).toBeCloseTo(b.z, 6);
    expect(a.w).toBeCloseTo(b.w, 6);
  });

  it("mesh pole offset maps sphere +Y to KSP north in body basis", () => {
    const pole = kspMeshPoleOffsetQuaternion();
    const mapped = new THREE.Vector3(0, 1, 0).applyQuaternion(pole);
    expect(angleBetweenDegrees(mapped, KSP_BODY_NORTH_LOCAL_THREE)).toBeLessThan(
      0.01,
    );
  });

  it("attitude maps north pole to same direction as vector path", () => {
    const kspQ = kspRootQuaternionObliquityTilt(Math.PI / 6, 0);
    const northKsp = kspNorthPoleRootDirection(kspQ);
    const threeQ = kspRootQuaternionToThree(kspQ);
    const pole = kspMeshPoleOffsetQuaternion();
    const northThree = new THREE.Vector3(0, 0, -1).applyQuaternion(threeQ);
    const expected = kspRootVectorToThree(northKsp);
    expect(angleBetweenDegrees(northThree, expected)).toBeLessThan(0.01);
    const meshNorth = new THREE.Vector3(0, 1, 0)
      .applyQuaternion(pole)
      .applyQuaternion(threeQ);
    expect(angleBetweenDegrees(meshNorth, expected)).toBeLessThan(0.01);
  });

  it("Three quaternion rotates body +X consistently with KSP apply", () => {
    const kspQ = kspRootQuaternionFromAxisAngle({ x: 0, y: 1, z: 0 }, Math.PI / 2);
    const bodyX = rotateKspRootVector(kspQ, { x: 1, y: 0, z: 0 });
    const threeQ = kspRootQuaternionToThree(kspQ);
    const threeX = new THREE.Vector3(1, 0, 0).applyQuaternion(threeQ);
    const expected = kspRootVectorToThree(bodyX);
    expect(angleBetweenDegrees(threeX, expected)).toBeLessThan(0.01);
  });
});
