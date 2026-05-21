import type { OrbitPatch, Vector3 } from "../telemetry/schema-v6";
import {
  conicToInertialSegments,
  inertialToPerifocal,
  perifocalToInertial,
  shouldSplitConicAtBodySurface,
} from "../math/buildConicGeometry";
import { findPatchRootAnchor } from "./patchAnchor";
import { distance3, maxConsecutiveLegMeters } from "../perf/pathSegments";
import { isRenderableVesselRootPath } from "../scene/vesselPathValidation";
import { finiteOr } from "../math/util";
import {
  rootRelativeToReferenceMathInertial,
  translateReferenceInertialToRoot,
} from "./referenceBodyFrames";
import type { BodyModel } from "../model/buildSolarSystemModel";

export type PatchConicDrawMode = "open" | "closed";

/** 3D map: math perifocal plane → KSP reference inertial (ecliptic in XZ). */
const PATCH_CONIC_KSP_AXIS_MAPPING = true;

const ELLIPTIC_RING_SAMPLES = 240;

function startTrueAnomalyRadians(patch: OrbitPatch): number {
  const degrees = patch.trueAnomalyDegrees;
  if (degrees == null || !Number.isFinite(degrees)) {
    return 0;
  }
  return (degrees * Math.PI) / 180;
}

export function trueAnomalyFromRootPosition(
  patch: OrbitPatch,
  anchor: Vector3 | null,
  targetRoot: Vector3,
): number | null {
  const mathInertial = rootRelativeToReferenceMathInertial(targetRoot, anchor);
  const perifocal = inertialToPerifocal(mathInertial, patch);
  if (!Number.isFinite(perifocal.x) || !Number.isFinite(perifocal.y)) {
    return null;
  }
  return Math.atan2(perifocal.y, perifocal.x);
}

function startTrueAnomalySearch(
  patch: OrbitPatch,
  anchor: Vector3 | null,
  targetRoot: Vector3,
): number | null {
  const eccentricity = finiteOr(patch.eccentricity, NaN);
  const semiLatusRectum = finiteOr(patch.semiLatusRectumMeters, NaN);
  if (
    !Number.isFinite(eccentricity) ||
    !Number.isFinite(semiLatusRectum) ||
    semiLatusRectum <= 0
  ) {
    return null;
  }

  const anchorVec = anchor ?? { x: 0, y: 0, z: 0 };
  let bestNu = startTrueAnomalyRadians(patch);
  let bestDistSq = Infinity;

  for (let i = 0; i < 720; i++) {
    const nu = (i / 720) * Math.PI * 2;
    const radius = semiLatusRectum / (1 + eccentricity * Math.cos(nu));
    const perifocal = {
      x: radius * Math.cos(nu),
      y: radius * Math.sin(nu),
      z: 0,
    };
    const inertial = perifocalToInertial(perifocal, patch);
    const root = translateReferenceInertialToRoot(
      [inertial],
      anchorVec,
      PATCH_CONIC_KSP_AXIS_MAPPING,
    )[0];
    const dx = root.x - targetRoot.x;
    const dy = root.y - targetRoot.y;
    const dz = root.z - targetRoot.z;
    const distSq = dx * dx + dy * dy + dz * dz;
    if (distSq < bestDistSq) {
      bestDistSq = distSq;
      bestNu = nu;
    }
  }

  return bestNu;
}

function resolveStartTrueAnomaly(
  patch: OrbitPatch,
  anchor: Vector3 | null,
  vesselRoot: Vector3,
): number {
  return (
    trueAnomalyFromRootPosition(patch, anchor, vesselRoot) ??
    startTrueAnomalySearch(patch, anchor, vesselRoot) ??
    startTrueAnomalyRadians(patch)
  );
}

function rootPointAtTrueAnomaly(
  patch: OrbitPatch,
  anchor: Vector3,
  nu: number,
): Vector3 {
  const eccentricity = finiteOr(patch.eccentricity, 0);
  const semiLatusRectum = finiteOr(patch.semiLatusRectumMeters, NaN);
  const radius = semiLatusRectum / (1 + eccentricity * Math.cos(nu));
  const perifocal = {
    x: radius * Math.cos(nu),
    y: radius * Math.sin(nu),
    z: 0,
  };
  const inertial = perifocalToInertial(perifocal, patch);
  return translateReferenceInertialToRoot(
    [inertial],
    anchor,
    PATCH_CONIC_KSP_AXIS_MAPPING,
  )[0];
}

/** Closed ellipse in root frame with index 0 at the vessel (smooth, no vertex snap). */
export function buildEllipticRingThroughVessel(
  patch: OrbitPatch,
  anchor: Vector3 | null,
  vesselRoot: Vector3,
): Vector3[] {
  const anchorVec = anchor ?? { x: 0, y: 0, z: 0 };
  const nu0 = resolveStartTrueAnomaly(patch, anchor, vesselRoot);
  const points: Vector3[] = [];

  for (let i = 0; i <= ELLIPTIC_RING_SAMPLES; i++) {
    const nu = nu0 + (2 * Math.PI * i) / ELLIPTIC_RING_SAMPLES;
    points.push(rootPointAtTrueAnomaly(patch, anchorVec, nu));
  }

  let nearest = 0;
  let nearestDist = Infinity;
  for (let i = 0; i < points.length; i++) {
    const d = distance3(points[i], vesselRoot);
    if (d < nearestDist) {
      nearestDist = d;
      nearest = i;
    }
  }

  return [...points.slice(nearest), ...points.slice(0, nearest)];
}

function isSupportedPatch(patch: OrbitPatch | null | undefined): boolean {
  return (
    patch?.classification === "Elliptic" ||
    patch?.classification === "HyperbolicEscape"
  );
}

const HELIOCENTRIC_ORBIT_RADIUS_METERS = 1e9;

/**
 * Patch used for the blue vessel orbit ring (not the yellow route overlay).
 * When the ship is heliocentric, prefer the Sun-centered elliptic leg in the chain
 * instead of patch 0 (often a SOI-local escape leg with a different anchor).
 */
export function resolveVesselOrbitDisplayPatch(
  patches: OrbitPatch[],
  vesselRoot: Vector3 | null,
): OrbitPatch | null {
  const supported = [...patches]
    .filter((p) => isSupportedPatch(p))
    .sort((a, b) => finiteOr(a.patchIndex, 0) - finiteOr(b.patchIndex, 0));
  if (supported.length === 0) {
    return null;
  }

  if (vesselRoot) {
    const radius = Math.hypot(vesselRoot.x, vesselRoot.y, vesselRoot.z);
    if (radius >= HELIOCENTRIC_ORBIT_RADIUS_METERS) {
      const sunElliptic = supported.find(
        (p) => p.referenceBody === "Sun" && p.classification === "Elliptic",
      );
      if (sunElliptic) {
        return sunElliptic;
      }
    }
  }

  const active = supported.find((p) => p.isActivePatch);
  return active ?? supported[0];
}

function placementPosition(
  patch: OrbitPatch,
  role: "patchStart" | "patchEnd" | "encounter",
): Vector3 | null {
  const sample = patch.placementSamples?.find((s) => s.sampleRole === role);
  return sample?.positionRootRelativeMeters ?? null;
}

/** Prograde sweep from nu0 to nu1 (KSP map shows forward along the orbit). */
function progradeTrueAnomalyDelta(nu0: number, nu1: number): number {
  let delta = nu1 - nu0;
  while (delta < 0) {
    delta += 2 * Math.PI;
  }
  while (delta >= 2 * Math.PI) {
    delta -= 2 * Math.PI;
  }
  if (Math.abs(delta) < 1e-6) {
    return Math.PI / 4;
  }
  return delta;
}

/** Partial ellipse arc between two root points (transfer leg, not a full period). */
export function buildEllipticArcBetweenRootPoints(
  patch: OrbitPatch,
  anchor: Vector3,
  fromRoot: Vector3,
  toRoot: Vector3,
  sampleCount = 120,
): Vector3[] {
  const nu0 = resolveStartTrueAnomaly(patch, anchor, fromRoot);
  const nu1 = resolveStartTrueAnomaly(patch, anchor, toRoot);
  const delta = progradeTrueAnomalyDelta(nu0, nu1);

  const points: Vector3[] = [];
  for (let i = 0; i <= sampleCount; i++) {
    const nu = nu0 + (delta * i) / sampleCount;
    points.push(rootPointAtTrueAnomaly(patch, anchor, nu));
  }
  return points;
}

/** Patches to draw for KSP-style trajectory preview (current leg + post-encounter SOI). */
export function patchesForTrajectoryPreview(
  patches: OrbitPatch[],
): OrbitPatch[] {
  const supported = [...patches]
    .filter((p) => isSupportedPatch(p))
    .sort((a, b) => finiteOr(a.patchIndex, 0) - finiteOr(b.patchIndex, 0));
  if (supported.length === 0) {
    return [];
  }
  const first = supported[0];
  if (!first.encounterBody) {
    return supported.slice(0, 1);
  }
  return supported.slice(0, Math.min(2, supported.length));
}

/**
 * Full patched-conic trajectory in root frame: current leg through encounter,
 * then the next SOI patch (e.g. Duna hyperbolic escape).
 */
export function buildTrajectoryPreviewSegments(
  patches: OrbitPatch[],
  bodyModels: BodyModel[],
  rootBodyName: string | null,
  vesselRoot: Vector3 | null,
  vesselPathPoints: Vector3[],
): Vector3[][] {
  const previewPatches = patchesForTrajectoryPreview(patches);
  const segments: Vector3[][] = [];

  previewPatches.forEach((patch, index) => {
    const anchor = findPatchRootAnchor(patch, bodyModels, rootBodyName);

    if (index === 0) {
      const encounter =
        placementPosition(patch, "encounter") ??
        placementPosition(patch, "patchEnd");

      if (vesselRoot && anchor && encounter) {
        segments.push(
          buildEllipticArcBetweenRootPoints(
            patch,
            anchor,
            vesselRoot,
            encounter,
            240,
          ),
        );
      } else if (
        vesselPathPoints.length >= 2 &&
        isRenderableVesselRootPath(vesselPathPoints)
      ) {
        segments.push(vesselPathPoints);
      } else if (vesselRoot && anchor) {
        segments.push(
          ...buildActivePatchConicRootSegments(
            patch,
            anchor,
            vesselRoot,
            "open",
          ),
        );
      }
      return;
    }

    const patchStart = placementPosition(patch, "patchStart");
    segments.push(
      ...buildActivePatchConicRootSegments(
        patch,
        anchor,
        patchStart,
        "open",
      ),
    );
  });

  return segments.filter((segment) => segment.length >= 2);
}

/** Prefer closed analytic ellipse; otherwise KSP samples or open conic fallback. */
export function buildActivePatchDisplaySegments(
  patch: OrbitPatch,
  anchor: Vector3 | null,
  vesselRoot: Vector3 | null,
  vesselPathPoints: Vector3[],
): Vector3[][] {
  if (patch.classification === "Elliptic" && vesselRoot) {
    return [buildEllipticRingThroughVessel(patch, anchor, vesselRoot)];
  }

  if (
    vesselPathPoints.length >= 2 &&
    isRenderableVesselRootPath(vesselPathPoints)
  ) {
    return [vesselPathPoints];
  }

  if (!vesselRoot) {
    return buildActivePatchConicRootSegments(patch, anchor, null, "open");
  }

  return buildActivePatchConicRootSegments(patch, anchor, vesselRoot, "open");
}

/** Hyperbolic / fallback analytic segments. */
export function buildActivePatchConicRootSegments(
  patch: OrbitPatch,
  anchor: Vector3 | null,
  alignToRoot: Vector3 | null = null,
  mode: PatchConicDrawMode = "open",
): Vector3[][] {
  const classification = patch.classification;
  if (classification !== "Elliptic" && classification !== "HyperbolicEscape") {
    return [];
  }

  if (classification === "Elliptic" && alignToRoot && mode === "closed") {
    return [buildEllipticRingThroughVessel(patch, anchor, alignToRoot)];
  }

  const bodyRadius = finiteOr(patch.referenceBodyRadiusMeters, 1000);
  const splitAtSurface = shouldSplitConicAtBodySurface(classification);
  const anchorVec = anchor ?? { x: 0, y: 0, z: 0 };

  let startNu = startTrueAnomalyRadians(patch);
  if (alignToRoot) {
    startNu = resolveStartTrueAnomaly(patch, anchor, alignToRoot);
  }

  const segments = conicToInertialSegments(patch, bodyRadius, splitAtSurface, {
    startTrueAnomalyRadians: startNu,
    sampleCount: ELLIPTIC_RING_SAMPLES,
  });

  return segments
    .filter((segment) => segment.length >= 2)
    .map((segment) =>
      translateReferenceInertialToRoot(
        segment,
        anchorVec,
        PATCH_CONIC_KSP_AXIS_MAPPING,
      ),
    );
}

/** Keep the vessel path leg that actually contains the ship (drops wedge chords). */
export function selectVesselPathLegContainingShip(
  points: Vector3[],
  vesselRoot: Vector3 | null,
  maxGapMeters = 5e10,
): Vector3[][] {
  if (points.length < 2) {
    return [];
  }
  if (!vesselRoot) {
    return [points];
  }

  const legs: Vector3[][] = [];
  let current: Vector3[] = [points[0]];

  for (let i = 1; i < points.length; i++) {
    const prev = points[i - 1];
    const next = points[i];
    if (distance3(prev, next) > maxGapMeters) {
      if (current.length >= 2) {
        legs.push(current);
      }
      current = [next];
    } else {
      current.push(next);
    }
  }
  if (current.length >= 2) {
    legs.push(current);
  }

  const nearVessel = (leg: Vector3[]) =>
    leg.some((p) => distance3(p, vesselRoot) < 5e8);

  const viable = legs.filter((leg) => {
    if (!nearVessel(leg) || leg.length < 2) {
      return false;
    }
    const radii = leg.map((p) => Math.hypot(p.x, p.y, p.z));
    const minR = Math.min(...radii);
    const maxR = Math.max(...radii);
    if (minR > 0 && maxR / minR > 200) {
      return false;
    }
    const maxLeg = maxConsecutiveLegMeters(leg);
    return maxLeg < maxR * 1.5;
  });

  if (viable.length === 0) {
    return [];
  }

  viable.sort((a, b) => b.length - a.length);
  return [viable[0]];
}
