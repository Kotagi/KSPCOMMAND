import type { CelestialBody, Vector3 } from "../../../telemetry/schema-v6";
import type { KspRootQuaternion } from "../../../coords/kspBodyOrientation";
import {
  kspNorthPoleRootDirection,
  kspRootQuaternionFromAxisAngle,
  multiplyKspRootQuaternions,
  normalizeKspRootQuaternion,
} from "../../../coords/kspBodyOrientation";

/** Telemetry orientation block (schema v10 — Phase 3.4). */
export interface PlanetBodyOrientationSnapshot {
  bodyOrientationReferenceFrame?: string;
  bodyOrientationSampleUniversalTimeSeconds?: number;
  rotates?: boolean;
  inverseRotation?: boolean;
  tidallyLocked?: boolean;
  rotationPeriodSeconds?: number;
  bodyOrientationRootRelative?: KspRootQuaternion;
  spinAxisRootRelative?: { x: number; y: number; z: number };
  angularVelocityRootRelativeRadPerSec?: { x: number; y: number; z: number };
  rotationAngleRadians?: number;
}

export type CelestialBodyWithOrientation = CelestialBody & PlanetBodyOrientationSnapshot;

export function isPlanetBodyOrientationReady(
  body: PlanetBodyOrientationSnapshot | undefined,
): body is PlanetBodyOrientationSnapshot & {
  bodyOrientationRootRelative: KspRootQuaternion;
} {
  const q = body?.bodyOrientationRootRelative;
  if (!q) {
    return false;
  }
  const len = Math.hypot(q.x, q.y, q.z, q.w);
  return Number.isFinite(len) && len > 0.01;
}

export function readPlanetBodyOrientation(
  body: CelestialBodyWithOrientation | undefined,
): KspRootQuaternion | undefined {
  if (!isPlanetBodyOrientationReady(body)) {
    return undefined;
  }
  return normalizeKspRootQuaternion(body.bodyOrientationRootRelative);
}

export function planetBodyAngularVelocity(
  body: CelestialBodyWithOrientation | undefined,
): Vector3 | undefined {
  return body?.angularVelocityRootRelativeRadPerSec;
}

export function planetBodyRotates(
  body: CelestialBodyWithOrientation | undefined,
): boolean {
  return body?.rotates !== false;
}

/**
 * Snapshot quaternion advanced to game UT: q(UT) = q_delta(ω̂, ‖ω‖·ΔUT) ⊗ q_sample.
 */
export function resolvePlanetBodyOrientationAtUt(
  body: CelestialBodyWithOrientation | undefined,
  gameUniversalTimeSeconds: number,
): KspRootQuaternion | undefined {
  const base = readPlanetBodyOrientation(body);
  if (!base) {
    return undefined;
  }
  if (!planetBodyRotates(body)) {
    return base;
  }

  const av = body?.angularVelocityRootRelativeRadPerSec;
  const sampleUt = body?.bodyOrientationSampleUniversalTimeSeconds;
  if (
    !av ||
    sampleUt == null ||
    !Number.isFinite(gameUniversalTimeSeconds) ||
    !Number.isFinite(sampleUt)
  ) {
    return base;
  }

  const spinSpeed = Math.hypot(av.x, av.y, av.z);
  if (spinSpeed <= 0) {
    return base;
  }

  const dt = gameUniversalTimeSeconds - sampleUt;
  if (Math.abs(dt) < 1e-12) {
    return base;
  }

  const sign = body?.inverseRotation === true ? -1 : 1;
  const northAxis = kspNorthPoleRootDirection(base);
  const deltaQ = kspRootQuaternionFromAxisAngle(northAxis, sign * spinSpeed * dt);
  return multiplyKspRootQuaternions(deltaQ, base);
}

/** Geographic spin axis from attitude (R · north), preferred over raw ω for display. */
export function planetBodySpinAxis(
  body: CelestialBodyWithOrientation | undefined,
  orientationKsp?: KspRootQuaternion,
): Vector3 | undefined {
  if (orientationKsp) {
    const north = kspNorthPoleRootDirection(orientationKsp);
    const len = Math.hypot(north.x, north.y, north.z);
    if (len > 1e-12) {
      return { x: north.x / len, y: north.y / len, z: north.z / len };
    }
  }
  const axis = body?.spinAxisRootRelative;
  if (!axis) {
    return undefined;
  }
  const len = Math.hypot(axis.x, axis.y, axis.z);
  if (len < 1e-12) {
    return undefined;
  }
  return { x: axis.x / len, y: axis.y / len, z: axis.z / len };
}
