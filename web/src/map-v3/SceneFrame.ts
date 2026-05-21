import type { Vector3 } from "../telemetry/schema-v6";
import {
  createSceneFrame,
  resolveSceneFrameFocus,
  toScenePoint as toScenePointV2,
  toScenePoints as toScenePointsV2,
} from "../map-v2/SceneFrame";
import type { MapContext } from "./MapContext";
import type { SceneFrameFocusMode, SceneFrameState, ScenePoint3 } from "./types";

export { resolveSceneFrameFocus, createSceneFrame };

export function toScenePoint(
  rootPoint: Vector3,
  frame: SceneFrameState,
): ScenePoint3 {
  return toScenePointV2(rootPoint, frame);
}

export function toScenePoints(
  points: Vector3[],
  frame: SceneFrameState,
): ScenePoint3[] {
  return toScenePointsV2(points, frame);
}

export type { MapContext, SceneFrameFocusMode, SceneFrameState, ScenePoint3 };
