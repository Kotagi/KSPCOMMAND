import { memo } from "react";
import { GradientDirectionalOrbitTrail } from "../../GradientDirectionalOrbitTrail";
import {
  PLANET_ORBIT_STYLE,
  resolvePlanetOrbitColor,
} from "../../../map-v3/elements/planetOrbit/planetOrbitStyle";
import { densifyPlanetOrbitScenePoints } from "../../../map-v3/elements/planetOrbit/densifyPlanetOrbitTrail";
import type { ScenePoint3 } from "../../../map-v3/types";

export const OrbitTrailV3 = memo(function OrbitTrailV3({
  lineKey,
  bodyName,
  points,
  anchorIndex = 0,
  sampleUniversalTimes,
  lineWidth = PLANET_ORBIT_STYLE.retrogradeLineWidth,
}: {
  lineKey: string;
  bodyName?: string;
  points: ScenePoint3[];
  anchorIndex?: number;
  sampleUniversalTimes?: number[];
  closedWithDuplicateEndpoint?: boolean;
  lineWidth?: number;
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

  const ring = densifyPlanetOrbitScenePoints(finitePoints);

  return (
    <GradientDirectionalOrbitTrail
      lineKey={lineKey}
      lineColor={resolvePlanetOrbitColor(bodyName)}
      points={ring}
      anchorIndex={anchorIndex}
      sampleUniversalTimes={sampleUniversalTimes}
      lineWidth={lineWidth}
      progradeLineWidthFactor={PLANET_ORBIT_STYLE.progradeLineWidthFactor}
    />
  );
});
