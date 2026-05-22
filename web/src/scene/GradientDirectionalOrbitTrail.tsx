import { Line } from "@react-three/drei";
import {
  hexToRgbaVertexColors,
  ORBIT_TRAIL_OPACITY_TRAILING,
  progradeHalfVertexOpacities,
} from "./orbitTrailDirectionStyle";
import { splitOrbitTrailHalves } from "./splitOrbitTrailHalves";

type Point3 = [number, number, number];

const LINE_COLOR_FOR_VERTEX_COLORS = "#ffffff";

/**
 * KSP map trails (v1 look + smooth prograde fade):
 * - Retrograde half: full body color, fully opaque
 * - Prograde half: same color, smooth opacity gradient bold at body → faint ahead
 */
export function GradientDirectionalOrbitTrail({
  lineKey,
  lineColor,
  points,
  anchorIndex = 0,
  lineWidth = 1,
  progradeLineWidthFactor = 0.7,
  closedWithDuplicateEndpoint = false,
  sampleUniversalTimes,
}: {
  lineKey: string;
  lineColor: string;
  points: Point3[];
  anchorIndex?: number;
  lineWidth?: number;
  progradeLineWidthFactor?: number;
  closedWithDuplicateEndpoint?: boolean;
  sampleUniversalTimes?: number[];
  useDenseRingHalves?: boolean;
}) {
  if (points.length < 2) {
    return null;
  }

  const { retrograde, prograde } = splitOrbitTrailHalves(
    points,
    anchorIndex,
    closedWithDuplicateEndpoint,
    sampleUniversalTimes,
  );

  const progradeWidth = lineWidth * progradeLineWidthFactor;
  const progradeOpacities = progradeHalfVertexOpacities(prograde.points.length);
  const progradeVertexColors = hexToRgbaVertexColors(
    lineColor,
    progradeOpacities,
  );

  return (
    <group>
      {retrograde.points.length >= 2 ? (
        <Line
          key={`${lineKey}-retro`}
          points={retrograde.points}
          color={lineColor}
          lineWidth={lineWidth}
          transparent
          opacity={ORBIT_TRAIL_OPACITY_TRAILING}
          toneMapped={false}
          depthWrite
        />
      ) : null}
      {prograde.points.length >= 2 ? (
        <Line
          key={`${lineKey}-pro`}
          points={prograde.points}
          color={LINE_COLOR_FOR_VERTEX_COLORS}
          vertexColors={progradeVertexColors}
          lineWidth={progradeWidth}
          toneMapped={false}
          transparent
          depthWrite
        />
      ) : null}
    </group>
  );
}
