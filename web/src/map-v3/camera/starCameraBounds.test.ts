import { describe, expect, it } from "vitest";
import {
  starMarkerDrawFrame,
  starMarkerSceneFrame,
} from "./starCameraBounds";
import type { SceneFrameState } from "../types";

const base: SceneFrameState = {
  focusMode: "body",
  focusBodyName: "Kerbin",
  focus: { x: 1e10, y: 2e9, z: -5e9 },
  displayScale: 1e-9,
};

describe("starMarkerDrawFrame", () => {
  it("clears focus in full-system view", () => {
    const frame: SceneFrameState = { ...base, focus: null, focusMode: "system" };
    const draw = starMarkerDrawFrame(frame);
    expect(draw.focus).toBeNull();
    expect(draw).toEqual(starMarkerSceneFrame(frame));
  });

  it("keeps world shift when display focus is set (body focus / SOI)", () => {
    const draw = starMarkerDrawFrame(base);
    expect(draw.focus).toEqual(base.focus);
    expect(draw.displayScale).toBe(base.displayScale);
  });
});
