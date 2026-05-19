import type { OrbitElements, Vector3 } from "../telemetry/schema-v6";
import { clamp, degreesToRadians, finiteOr, isFiniteNumber } from "./util";

export type ProjectionMode =
  | "orbitPlane"
  | "referenceBodyInertialXZ"
  | "referenceBodyInertialXY"
  | "rootXZ"
  | "rootXY";

export interface ProjectedPoint {
  x: number;
  y: number;
}

export interface ConicPoint {
  trueAnomalyRadians: number;
  radiusMeters: number;
  perifocal: Vector3;
  inertial: Vector3;
  projected: ProjectedPoint;
}

export interface ConicGeometry {
  canDraw: boolean;
  reason: string;
  points: ConicPoint[];
  segments: ConicPoint[][];
  periapsisPoint: ConicPoint | null;
  apoapsisPoint: ConicPoint | null;
}

export function calculateSemiLatusRectum(
  semiMajorAxis: number,
  eccentricity: number,
): number {
  if (!isFiniteNumber(semiMajorAxis) || !isFiniteNumber(eccentricity)) {
    return NaN;
  }
  if (eccentricity < 1) {
    return semiMajorAxis * (1 - eccentricity * eccentricity);
  }
  if (eccentricity > 1) {
    return Math.abs(semiMajorAxis) * (eccentricity * eccentricity - 1);
  }
  return NaN;
}

export function perifocalToInertial(point: Vector3, orbit: OrbitElements | null | undefined): Vector3 {
  const lan = degreesToRadians(finiteOr(orbit?.longitudeOfAscendingNodeDegrees, 0));
  const inclination = degreesToRadians(finiteOr(orbit?.inclinationDegrees, 0));
  const argumentOfPeriapsis = degreesToRadians(finiteOr(orbit?.argumentOfPeriapsisDegrees, 0));
  const cosW = Math.cos(argumentOfPeriapsis);
  const sinW = Math.sin(argumentOfPeriapsis);
  const cosI = Math.cos(inclination);
  const sinI = Math.sin(inclination);
  const cosO = Math.cos(lan);
  const sinO = Math.sin(lan);
  const x1 = cosW * point.x - sinW * point.y;
  const y1 = sinW * point.x + cosW * point.y;
  const x2 = x1;
  const y2 = cosI * y1;
  const z2 = sinI * y1;
  return {
    x: cosO * x2 - sinO * y2,
    y: sinO * x2 + cosO * y2,
    z: z2,
  };
}

export function projectPoint(
  perifocal: Vector3 | null,
  inertial: Vector3,
  projectionMode: ProjectionMode,
): ProjectedPoint {
  if (projectionMode === "referenceBodyInertialXZ") {
    return { x: inertial.x, y: -inertial.z };
  }
  if (projectionMode === "referenceBodyInertialXY") {
    return { x: inertial.x, y: -inertial.y };
  }
  if (perifocal) {
    return { x: perifocal.x, y: -perifocal.y };
  }
  return { x: inertial.x, y: -inertial.z };
}

export function trueAnomalyAtRadius(
  semiLatusRectum: number,
  eccentricity: number,
  radius: number,
): number {
  if (
    !isFiniteNumber(radius) ||
    radius <= 0 ||
    !isFiniteNumber(semiLatusRectum) ||
    !isFiniteNumber(eccentricity) ||
    eccentricity === 0
  ) {
    return NaN;
  }
  const value = (semiLatusRectum / radius - 1) / eccentricity;
  if (value < -1 || value > 1) {
    return NaN;
  }
  return Math.acos(clamp(value, -1, 1));
}

function emptyGeometry(reason: string): ConicGeometry {
  return {
    canDraw: false,
    reason,
    points: [],
    segments: [],
    periapsisPoint: null,
    apoapsisPoint: null,
  };
}

function buildConicPoint(
  orbit: OrbitElements | null | undefined,
  semiLatusRectum: number,
  eccentricity: number,
  trueAnomalyRadians: number,
  projectionMode: ProjectionMode,
): ConicPoint | null {
  const denominator = 1 + eccentricity * Math.cos(trueAnomalyRadians);
  if (Math.abs(denominator) < 1e-9) {
    return null;
  }
  const radius = semiLatusRectum / denominator;
  if (!isFiniteNumber(radius) || radius <= 0) {
    return null;
  }
  const perifocal: Vector3 = {
    x: radius * Math.cos(trueAnomalyRadians),
    y: radius * Math.sin(trueAnomalyRadians),
    z: 0,
  };
  const inertial = perifocalToInertial(perifocal, orbit);
  const projected = projectPoint(perifocal, inertial, projectionMode);
  return {
    trueAnomalyRadians,
    radiusMeters: radius,
    perifocal,
    inertial,
    projected,
  };
}

function sampleEllipsePoints(
  orbit: OrbitElements,
  semiLatusRectum: number,
  eccentricity: number,
  sampleCount: number,
  projectionMode: ProjectionMode,
): ConicPoint[] {
  const points: ConicPoint[] = [];
  for (let i = 0; i <= sampleCount; i++) {
    const point = buildConicPoint(
      orbit,
      semiLatusRectum,
      eccentricity,
      (Math.PI * 2 * i) / sampleCount,
      projectionMode,
    );
    if (point) {
      points.push(point);
    }
  }
  return points;
}

function sampleHyperbolaPoints(
  orbit: OrbitElements,
  bodyRadius: number,
  semiLatusRectum: number,
  eccentricity: number,
  sampleCount: number,
  projectionMode: ProjectionMode,
): ConicPoint[] {
  const points: ConicPoint[] = [];
  const epsilon = 0.035;
  let anomalyLimit = Math.acos(clamp(-1 / eccentricity, -1, 1)) - epsilon;
  const soi = finiteOr(orbit.sphereOfInfluenceMeters, NaN);
  const displayRadius =
    isFiniteNumber(soi) && soi > bodyRadius
      ? soi
      : Math.max(bodyRadius * 12, finiteOr(orbit.periapsisRadiusMeters, bodyRadius) * 8);
  const soiLimit = trueAnomalyAtRadius(semiLatusRectum, eccentricity, displayRadius);
  if (isFiniteNumber(soiLimit) && soiLimit > 0) {
    anomalyLimit = Math.min(anomalyLimit, soiLimit);
  }
  anomalyLimit = clamp(anomalyLimit, 0.15, Math.PI - epsilon);
  for (let i = 0; i <= sampleCount; i++) {
    const t = i / sampleCount;
    const anomaly = -anomalyLimit + anomalyLimit * 2 * t;
    const point = buildConicPoint(orbit, semiLatusRectum, eccentricity, anomaly, projectionMode);
    if (point && point.radiusMeters <= displayRadius * 1.01) {
      points.push(point);
    }
  }
  return points;
}

function splitSegmentsAboveRadius(points: ConicPoint[], bodyRadius: number): ConicPoint[][] {
  const segments: ConicPoint[][] = [];
  let current: ConicPoint[] = [];
  points.forEach((point) => {
    if (point.radiusMeters >= bodyRadius) {
      current.push(point);
      return;
    }
    if (current.length > 1) {
      segments.push(current);
    }
    current = [];
  });
  if (current.length > 1) {
    segments.push(current);
  }
  return segments;
}

export function buildConicGeometry(
  orbit: OrbitElements | null | undefined,
  bodyRadius: number,
  projectionMode: ProjectionMode = "orbitPlane",
  splitAtBodySurface = true,
): ConicGeometry {
  const eccentricity = finiteOr(orbit?.eccentricity, NaN);
  const semiMajorAxis = finiteOr(orbit?.semiMajorAxisMeters, NaN);
  const semiLatusRectum = finiteOr(
    orbit?.semiLatusRectumMeters,
    calculateSemiLatusRectum(semiMajorAxis, eccentricity),
  );
  const sampleCount = eccentricity > 1 ? 180 : 240;

  if (!isFiniteNumber(eccentricity) || eccentricity < 0) {
    return emptyGeometry("Orbit eccentricity is unavailable.");
  }
  if (!isFiniteNumber(semiLatusRectum) || semiLatusRectum <= 0) {
    return emptyGeometry("Conic semi-latus rectum is unavailable.");
  }

  let points: ConicPoint[];
  if (eccentricity < 1) {
    points = sampleEllipsePoints(orbit!, semiLatusRectum, eccentricity, sampleCount, projectionMode);
  } else if (eccentricity > 1) {
    points = sampleHyperbolaPoints(
      orbit!,
      bodyRadius,
      semiLatusRectum,
      eccentricity,
      sampleCount,
      projectionMode,
    );
  } else {
    return emptyGeometry("Parabolic trajectories are deferred for this phase.");
  }

  if (points.length < 2) {
    return emptyGeometry("Conic sampling produced too few drawable points.");
  }

  const segments = splitAtBodySurface ? splitSegmentsAboveRadius(points, bodyRadius) : [points];
  if (segments.length === 0) {
    return emptyGeometry("The sampled trajectory is below the reference body surface.");
  }

  return {
    canDraw: true,
    reason: "ok",
    points,
    segments,
    periapsisPoint: buildConicPoint(orbit, semiLatusRectum, eccentricity, 0, projectionMode),
    apoapsisPoint:
      eccentricity < 1
        ? buildConicPoint(orbit, semiLatusRectum, eccentricity, Math.PI, projectionMode)
        : null,
  };
}

/** 3D positions in reference-body inertial frame (meters). */
export function conicToInertialPath(
  orbit: OrbitElements | null | undefined,
  bodyRadius: number,
  maxPoints = 256,
): Vector3[] {
  const geometry = buildConicGeometry(orbit, bodyRadius, "orbitPlane", true);
  if (!geometry.canDraw) {
    return [];
  }
  const path: Vector3[] = [];
  geometry.segments.forEach((segment) => {
    segment.forEach((point) => {
      path.push({ ...point.inertial });
    });
  });
  if (path.length > maxPoints) {
    const step = path.length / maxPoints;
    const reduced: Vector3[] = [];
    for (let i = 0; i < maxPoints; i++) {
      reduced.push(path[Math.floor(i * step)]);
    }
    return reduced;
  }
  return path;
}
