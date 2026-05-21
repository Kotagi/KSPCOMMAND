import { memo } from "react";
import { Line } from "@react-three/drei";
import { splitOrbitTrailHalves } from "../../splitOrbitTrailHalves";
import { getKspBodyMapColor } from "../../bodyMapColors";
import type { ScenePoint3 } from "../../../map-v2/types";

export const OrbitTrailV2 = memo(function OrbitTrailV2({
  lineKey,
  bodyName,
  points,
  anchorIndex = 0,
  closedWithDuplicateEndpoint = false,
  lineWidth = 1,
}: {
  lineKey: string;
  bodyName?: string;
  points: ScenePoint3[];
  anchorIndex?: number;
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
  const lineColor = getKspBodyMapColor(bodyName);
  const { retrograde, prograde } = splitOrbitTrailHalves(
    finitePoints,
    anchorIndex,
    closedWithDuplicateEndpoint,
  );

  return (
    <group>
      {retrograde.points.length >= 2 && (
        <Line
          key={`${lineKey}-retro`}
          points={retrograde.points}
          color={lineColor}
          lineWidth={lineWidth}
          transparent
          opacity={retrograde.opacity}
        />
      )}
      {prograde.points.length >= 2 && (
        <Line
          key={`${lineKey}-pro`}
          points={prograde.points}
          color={lineColor}
          lineWidth={lineWidth * 0.7}
          transparent
          opacity={prograde.opacity}
        />
      )}
    </group>
  );
});
