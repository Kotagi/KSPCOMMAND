import type { ScenePoint3 } from "../map-v3/types";

/** Contiguous slice around a closed ring (uses every densified vertex, step = 1). */
export function ringHalfFromDenseRing(
  points: ScenePoint3[],
  anchorIndex: number,
  forward: boolean,
): ScenePoint3[] {
  const n = points.length;
  if (n < 2) {
    return points;
  }
  const anchor = ((anchorIndex % n) + n) % n;
  const count = Math.floor(n / 2) + 1;
  const out: ScenePoint3[] = [];
  for (let i = 0; i < count; i++) {
    const idx = forward
      ? (anchor + i) % n
      : (anchor - i + n) % n;
    out.push(points[idx]);
  }
  return out;
}
