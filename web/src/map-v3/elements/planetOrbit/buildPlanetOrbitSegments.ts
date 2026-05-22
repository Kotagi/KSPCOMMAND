import { buildSegments as buildV2Segments } from "../../../map-v2/TrajectoryPlanner";
import type { MapContext } from "../../MapContext";
import { isPlanetBody } from "../../MapContext";
import type { TrajectorySegment } from "../../types";
import {
  densifyPlanetOrbitRootPoints,
  densifyPlanetOrbitSampleUniversalTimes,
  resolvePlanetOrbitPointsFromPath,
  resolvePlanetOrbitSourcePoints,
  sampleUniversalTimesFromPath,
} from "./densifyPlanetOrbitTrail";

function nearestIndex(
  points: { x: number; y: number; z: number }[],
  target: { x: number; y: number; z: number },
): number {
  let best = 0;
  let bestDist = Infinity;
  points.forEach((p, i) => {
    const d = Math.hypot(p.x - target.x, p.y - target.y, p.z - target.z);
    if (d < bestDist) {
      bestDist = d;
      best = i;
    }
  });
  return best;
}

/**
 * Heliocentric planet trails only — delegates path geometry to v2 BodyOrbit planner
 * (samples + analytic fallback), maps role → v3 kind, then resamples to 512 verts.
 */
export function buildPlanetOrbitSegments(ctx: MapContext): TrajectorySegment[] {
  const v2 = buildV2Segments(ctx, "BodyOrbit", { planetOnly: true });
  return v2
    .filter((seg) => {
      const name = seg.bodyName;
      if (!name || name === ctx.rootBody) {
        return false;
      }
      return isPlanetBody(ctx, name);
    })
    .map((seg) => {
      const source = resolvePlanetOrbitSourcePoints(
        ctx,
        seg.bodyName,
        seg.points,
      );
      const points = densifyPlanetOrbitRootPoints(source);
      const bodyEntry = seg.bodyName
        ? ctx.bodyByName.get(seg.bodyName)
        : null;
      const anchorIndex = bodyEntry
        ? nearestIndex(points, bodyEntry.position)
        : (seg.anchorIndex ?? 0);

      let sampleUniversalTimes: number[] | undefined;
      if (seg.bodyName) {
        const path = (ctx.telemetry.bodyOrbitPaths ?? []).find(
          (p) => p.bodyName === seg.bodyName,
        );
        if (path) {
          const bodies = ctx.bodies.map((b) => ({
            body: { name: b.name },
            position: b.position,
          }));
          const analytic = resolvePlanetOrbitPointsFromPath(
            path,
            bodies,
            ctx.rootBody,
            seg.points,
          );
          const usesAnalytic = analytic != null && analytic.length >= 3;
          if (!usesAnalytic) {
            const raw = sampleUniversalTimesFromPath(path);
            if (raw) {
              sampleUniversalTimes = densifyPlanetOrbitSampleUniversalTimes(raw);
            }
          }
        }
      }

      return {
        kind: "planetOrbit" as const,
        key: seg.key,
        points,
        bodyName: seg.bodyName,
        referenceBody: seg.referenceBody,
        parentBody: seg.parentBody,
        closed: true,
        anchorIndex,
        sampleUniversalTimes,
        closedWithDuplicateEndpoint: false,
        lineWidth: seg.lineWidth,
        color: seg.color,
        opacity: seg.opacity,
        dashed: seg.dashed,
      };
    });
}
