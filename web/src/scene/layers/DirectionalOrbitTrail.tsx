import { Line } from "@react-three/drei";
import { getKspBodyMapColor } from "../bodyMapColors";
import { splitOrbitTrailHalves } from "../splitOrbitTrailHalves";

type Point3 = [number, number, number];

export interface DirectionalOrbitTrailProps {
  lineKey: string;
  bodyName?: string;
  points: Point3[];
  lineWidth?: number;
  anchorIndex?: number;
  closedWithDuplicateEndpoint?: boolean;
  iconPosition?: Point3 | null;
  sampleUniversalTimes?: number[];
}

/**
 * Per-body KSP map color: one smooth polyline per half (retrograde solid, prograde faint).
 */
export function DirectionalOrbitTrail({
  lineKey,
  bodyName,
  points,
  lineWidth = 1,
  anchorIndex = 0,
  closedWithDuplicateEndpoint = false,
  sampleUniversalTimes,
}: DirectionalOrbitTrailProps) {
  if (points.length < 2) {
    return null;
  }

  const lineColor = getKspBodyMapColor(bodyName);

  const { retrograde, prograde } = splitOrbitTrailHalves(
    points,
    anchorIndex,
    closedWithDuplicateEndpoint,
    sampleUniversalTimes,
  );

  const halves: { points: Point3[]; opacity: number; key: string }[] = [];
  if (retrograde.points.length >= 2) {
    halves.push({
      key: `${lineKey}-retro`,
      points: retrograde.points,
      opacity: retrograde.opacity,
    });
  }
  if (prograde.points.length >= 2) {
    halves.push({
      key: `${lineKey}-pro`,
      points: prograde.points,
      opacity: prograde.opacity,
    });
  }

  if (halves.length === 0 && points.length >= 2) {
    return (
      <Line
        points={points}
        color={lineColor}
        lineWidth={lineWidth}
        transparent
        opacity={0.52}
      />
    );
  }

  return (
    <group>
      {halves.map((half) => (
        <Line
          key={half.key}
          points={half.points}
          color={lineColor}
          lineWidth={lineWidth}
          transparent
          opacity={half.opacity}
        />
      ))}
    </group>
  );
}
