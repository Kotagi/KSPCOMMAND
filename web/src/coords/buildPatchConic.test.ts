import { describe, expect, it } from "vitest";
import { perifocalToInertial, inertialToPerifocal } from "../math/buildConicGeometry";
import { kspRootToThree } from "./kspToThree";
import {
  buildActivePatchConicRootSegments,
  buildEllipticArcBetweenRootPoints,
  buildEllipticRingThroughVessel,
  buildTrajectoryPreviewSegments,
  patchesForTrajectoryPreview,
  resolveVesselOrbitDisplayPatch,
  trueAnomalyFromRootPosition,
} from "./buildPatchConic";
import { maxConsecutiveLegMeters } from "../perf/pathSegments";

describe("buildActivePatchConic", () => {
  it("places equatorial Sun orbit in KSP XZ (ecliptic), not KSP XY", () => {
    const patch = {
      classification: "Elliptic",
      referenceBody: "Sun",
      referenceBodyRadiusMeters: 696000000,
      eccentricity: 0.2,
      semiMajorAxisMeters: 1e11,
      semiLatusRectumMeters: 9.6e10,
      inclinationDegrees: 0,
      longitudeOfAscendingNodeDegrees: 0,
      argumentOfPeriapsisDegrees: 0,
      trueAnomalyDegrees: 0,
    };
    const segments = buildActivePatchConicRootSegments(patch, { x: 0, y: 0, z: 0 });
    const points = segments.flat();
    expect(points.length).toBeGreaterThan(20);

    const maxAbsY = Math.max(...points.map((p) => Math.abs(p.y)));
    const maxAbsX = Math.max(...points.map((p) => Math.abs(p.x)));
    const maxAbsZ = Math.max(...points.map((p) => Math.abs(p.z)));
    expect(maxAbsY).toBeLessThan(maxAbsX * 0.05);
    expect(maxAbsY).toBeLessThan(maxAbsZ * 0.05);
  });

  it("inertialToPerifocal inverts perifocalToInertial", () => {
    const orbit = {
      eccentricity: 0.3,
      inclinationDegrees: 12,
      longitudeOfAscendingNodeDegrees: 45,
      argumentOfPeriapsisDegrees: 10,
    };
    const perifocal = { x: 1e11, y: 2e10, z: 0 };
    const inertial = perifocalToInertial(perifocal, orbit);
    const back = inertialToPerifocal(inertial, orbit);
    expect(back.x).toBeCloseTo(perifocal.x, -6);
    expect(back.y).toBeCloseTo(perifocal.y, -6);
  });

  it("buildEllipticRingThroughVessel stays on the analytic ellipse near the vessel", () => {
    const patch = {
      classification: "Elliptic",
      referenceBody: "Sun",
      referenceBodyRadiusMeters: 696000000,
      eccentricity: 0.2,
      semiLatusRectumMeters: 9.6e10,
      inclinationDegrees: 5,
      longitudeOfAscendingNodeDegrees: 30,
      argumentOfPeriapsisDegrees: 15,
      trueAnomalyDegrees: 40,
    };
    const anchor = { x: 0, y: 0, z: 0 };
    const vessel = buildActivePatchConicRootSegments(patch, anchor, null)[0][60];
    const ring = buildEllipticRingThroughVessel(patch, anchor, vessel);
    const nearest = ring.reduce(
      (best, p) => {
        const d = Math.hypot(p.x - vessel.x, p.y - vessel.y, p.z - vessel.z);
        return d < best.d ? { d, p } : best;
      },
      { d: Infinity, p: ring[0] },
    );
    expect(nearest.d).toBeLessThan(1e8);
    const maxLeg = maxConsecutiveLegMeters(ring);
    expect(maxLeg).toBeLessThan(2e10);
  });

  it("trueAnomalyFromRootPosition matches a point on the sampled ring", () => {
    const patch = {
      classification: "Elliptic",
      referenceBody: "Sun",
      referenceBodyRadiusMeters: 696000000,
      eccentricity: 0.2,
      semiLatusRectumMeters: 9.6e10,
      inclinationDegrees: 5,
      longitudeOfAscendingNodeDegrees: 30,
      argumentOfPeriapsisDegrees: 15,
      trueAnomalyDegrees: 40,
    };
    const anchor = { x: 0, y: 0, z: 0 };
    const target = buildActivePatchConicRootSegments(patch, anchor, null)[0][12];
    const nu = trueAnomalyFromRootPosition(patch, anchor, target);
    expect(nu).not.toBeNull();
    const aligned = buildEllipticRingThroughVessel(patch, anchor, target);
    expect(aligned[0].x).toBe(target.x);
  });

  it("resolveVesselOrbitDisplayPatch prefers Sun elliptic when vessel is heliocentric", () => {
    const patches = [
      {
        patchIndex: 0,
        isActivePatch: true,
        classification: "HyperbolicEscape",
        referenceBody: "Kerbin",
        encounterBody: "Minmus",
      },
      {
        patchIndex: 1,
        classification: "Elliptic",
        referenceBody: "Sun",
        encounterBody: "Duna",
      },
    ];
    const vessel = { x: 5e10, y: 0, z: 1e10 };
    const display = resolveVesselOrbitDisplayPatch(patches, vessel);
    expect(display?.referenceBody).toBe("Sun");
    expect(display?.classification).toBe("Elliptic");
  });

  it("includes current patch and post-encounter SOI for trajectory preview", () => {
    const patches = [
      {
        patchIndex: 0,
        classification: "Elliptic",
        referenceBody: "Sun",
        encounterBody: "Duna",
        placementSamples: [
          {
            sampleRole: "patchEnd",
            positionRootRelativeMeters: { x: 1e11, y: 0, z: 0 },
          },
        ],
      },
      {
        patchIndex: 1,
        classification: "HyperbolicEscape",
        referenceBody: "Duna",
        referenceBodyRadiusMeters: 3.4e5,
        eccentricity: 1.2,
        semiLatusRectumMeters: 5e7,
        placementSamples: [
          {
            sampleRole: "patchStart",
            positionRootRelativeMeters: { x: 1e11, y: 0, z: 0 },
          },
        ],
      },
      {
        patchIndex: 2,
        classification: "Elliptic",
        referenceBody: "Sun",
      },
    ];
    const preview = patchesForTrajectoryPreview(patches);
    expect(preview.length).toBe(2);
    expect(preview[0].patchIndex).toBe(0);
    expect(preview[1].referenceBody).toBe("Duna");
  });

  it("buildEllipticArcBetweenRootPoints uses prograde sweep, not the retrograde chord", () => {
    const patch = {
      classification: "Elliptic",
      referenceBody: "Sun",
      referenceBodyRadiusMeters: 696000000,
      eccentricity: 0.2,
      semiLatusRectumMeters: 9.6e10,
      inclinationDegrees: 0,
      longitudeOfAscendingNodeDegrees: 0,
      argumentOfPeriapsisDegrees: 0,
      trueAnomalyDegrees: 0,
    };
    const anchor = { x: 0, y: 0, z: 0 };
    const ring = buildActivePatchConicRootSegments(patch, anchor, null)[0];
    const from = ring[40];
    const to = ring[200];
    const arc = buildEllipticArcBetweenRootPoints(patch, anchor, from, to, 180);
    expect(arc.length).toBeGreaterThan(100);
    const maxLeg = maxConsecutiveLegMeters(arc);
    const span = Math.hypot(to.x - from.x, to.y - from.y, to.z - from.z);
    expect(maxLeg).toBeLessThan(span * 0.25);
  });

  it("trajectory preview draws analytic arc to encounter, not a chord from sparse samples", () => {
    const patch = {
      patchIndex: 0,
      classification: "Elliptic",
      referenceBody: "Sun",
      referenceBodyRadiusMeters: 696000000,
      eccentricity: 0.55,
      semiLatusRectumMeters: 8e10,
      inclinationDegrees: 0,
      longitudeOfAscendingNodeDegrees: 0,
      argumentOfPeriapsisDegrees: 0,
      trueAnomalyDegrees: 20,
      encounterBody: "Duna",
      referenceBodyPositionRootRelativeMeters: { x: 0, y: 0, z: 0 },
    };
    const anchor = { x: 0, y: 0, z: 0 };
    const ring = buildActivePatchConicRootSegments(patch, anchor, null)[0];
    const vessel = ring[50];
    const encounter = ring[190];
    const sparsePath = [vessel, ring[55]];
    const segments = buildTrajectoryPreviewSegments(
      [
        {
          ...patch,
          placementSamples: [
            {
              sampleRole: "encounter",
              positionRootRelativeMeters: encounter,
            },
          ],
        },
      ],
      [{ body: { name: "Sun" }, position: anchor, projected: { x: 0, y: 0 }, isScrubPreview: false }],
      "Sun",
      vessel,
      sparsePath,
    );
    expect(segments.length).toBe(1);
    const arc = segments[0];
    expect(arc.length).toBeGreaterThan(100);
    const chord = Math.hypot(
      encounter.x - vessel.x,
      encounter.y - vessel.y,
      encounter.z - vessel.z,
    );
    expect(maxConsecutiveLegMeters(arc)).toBeLessThan(chord * 0.2);
  });

  it("maps conic into Three.js ecliptic XY plane via kspRootToThree", () => {
    const patch = {
      classification: "Elliptic",
      referenceBody: "Sun",
      referenceBodyRadiusMeters: 696000000,
      eccentricity: 0.1,
      semiLatusRectumMeters: 1e11,
      inclinationDegrees: 0,
      longitudeOfAscendingNodeDegrees: 0,
      argumentOfPeriapsisDegrees: 0,
      trueAnomalyDegrees: 45,
    };
    const points = buildActivePatchConicRootSegments(patch, { x: 0, y: 0, z: 0 }).flat();
    const three = points.map((p) => kspRootToThree(p));
    const maxAbsZ = Math.max(...three.map((p) => Math.abs(p[2])));
    const maxAbsX = Math.max(...three.map((p) => Math.abs(p[0])));
    const maxAbsY = Math.max(...three.map((p) => Math.abs(p[1])));
    expect(maxAbsZ).toBeLessThan(maxAbsX * 0.05);
    expect(maxAbsZ).toBeLessThan(maxAbsY * 0.05);
  });
});
