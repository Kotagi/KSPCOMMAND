import type { Vector3 } from "../telemetry/schema-v6";

/** Reduce polyline point count by uniform stride sampling. */
export function decimatePath(points: Vector3[], maxPoints: number): Vector3[] {
  if (points.length <= maxPoints || maxPoints < 2) {
    return points;
  }
  const result: Vector3[] = [];
  const step = (points.length - 1) / (maxPoints - 1);
  for (let i = 0; i < maxPoints; i++) {
    const idx = Math.min(Math.round(i * step), points.length - 1);
    result.push(points[idx]);
  }
  return result;
}
