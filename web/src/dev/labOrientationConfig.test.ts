import { describe, expect, it } from "vitest";
import { parseLabOrientationFromSearch } from "./labOrientationConfig";
import { rotateKspRootVector } from "../coords/kspBodyOrientation";

describe("labOrientationConfig", () => {
  it("defaults to identity when orientation param absent", () => {
    const cfg = parseLabOrientationFromSearch("");
    expect(cfg.preset).toBe("none");
    expect(cfg.orientationKsp.w).toBeCloseTo(1, 6);
    expect(cfg.extrapolateSpin).toBe(false);
  });

  it("parses tilt-x-90 preset", () => {
    const cfg = parseLabOrientationFromSearch("?orientation=tilt-x-90");
    expect(cfg.preset).toBe("tilt-x-90");
    const north = rotateKspRootVector(cfg.orientationKsp, { x: 0, y: 1, z: 0 });
    expect(Math.abs(north.z)).toBeGreaterThan(0.99);
  });

  it("parses custom quaternion and angular velocity", () => {
    const cfg = parseLabOrientationFromSearch(
      "?orientation=custom&qx=0&qy=0&qz=0&qw=1&avy=0.001&spin=1",
    );
    expect(cfg.preset).toBe("custom");
    expect(cfg.angularVelocityKsp?.y).toBeCloseTo(0.001, 6);
    expect(cfg.extrapolateSpin).toBe(true);
  });

  it("enables spin on tilt-and-spin preset", () => {
    const cfg = parseLabOrientationFromSearch("?orientation=tilt-and-spin");
    expect(cfg.preset).toBe("tilt-and-spin");
    expect(cfg.extrapolateSpin).toBe(true);
    expect(cfg.angularVelocityKsp?.y).toBeGreaterThan(0);
  });
});
