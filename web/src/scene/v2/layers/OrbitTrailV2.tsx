import { memo } from "react";
import { Line } from "@react-three/drei";
import { ringHalfFromDenseRing } from "../../planetOrbitRingHalves";
import { splitOrbitTrailHalves } from "../../splitOrbitTrailHalves";
import { densifyPlanetOrbitScenePoints } from "../../../map-v3/elements/planetOrbit/densifyPlanetOrbitTrail";
import { PLANET_ORBIT_STYLE } from "../../../map-v3/elements/planetOrbit/planetOrbitStyle";
import { getKspBodyMapColor } from "../../bodyMapColors";
import type { ScenePoint3 } from "../../../map-v2/types";

export const OrbitTrailV2 = memo(function OrbitTrailV2({
  lineKey,
  bodyName,
  points,
  anchorIndex = 0,
  closedWithDuplicateEndpoint = false,
  lineWidth = 1,
  /** When true, resample to 512 verts and draw contiguous ring halves. */
  planetRing = false,
}: {
  lineKey: string;
  bodyName?: string;
  points: ScenePoint3[];
  anchorIndex?: number;
  closedWithDuplicateEndpoint?: boolean;
  lineWidth?: number;
  planetRing?: boolean;
}) {
  const finitePoints = points.filter(
    (p) =>
      p.length >= 3 &&
      Number.isFinite(p[0]) &&
      Number.isFinite(p[1]) &&
      Number.isFinite(p[2]),
  );
  if (finitePoints.length < 2) {
    return null;
  }

  const lineColor = getKspBodyMapColor(bodyName);
  const ring = planetRing
    ? densifyPlanetOrbitScenePoints(finitePoints)
    : finitePoints;

  const halves =
    planetRing && ring.length >= PLANET_ORBIT_STYLE.trailVertices / 2
      ? {
          retrograde: {
            points: ringHalfFromDenseRing(ring, anchorIndex, false),
            opacity: 1,
          },
          prograde: {
            points: ringHalfFromDenseRing(ring, anchorIndex, true),
            opacity: 0.2,
          },
        }
      : splitOrbitTrailHalves(
          ring,
          anchorIndex,
          closedWithDuplicateEndpoint,
        );

  return (
    <group>
      {halves.retrograde.points.length >= 2 && (
        <Line
          key={`${lineKey}-retro`}
          points={halves.retrograde.points}
          color={lineColor}
          lineWidth={lineWidth}
          transparent
          opacity={halves.retrograde.opacity}
        />
      )}
      {halves.prograde.points.length >= 2 && (
        <Line
          key={`${lineKey}-pro`}
          points={halves.prograde.points}
          color={lineColor}
          lineWidth={lineWidth * 0.7}
          transparent
          opacity={halves.prograde.opacity}
        />
      )}
    </group>
  );
});
