import { describe, expect, it } from "vitest";
import { planetsInMeshModeFromDrawModes } from "./PlanetBodyMeshLodContext";

describe("planetsInMeshModeFromDrawModes", () => {
  it("returns only planets registered as mesh (moon orbit visibility gate)", () => {
    const modes = new Map<string, "icon" | "mesh">([
      ["Kerbin", "mesh"],
      ["Jool", "icon"],
      ["Eve", "mesh"],
    ]);
    const mesh = planetsInMeshModeFromDrawModes(modes);
    expect(mesh.has("Kerbin")).toBe(true);
    expect(mesh.has("Eve")).toBe(true);
    expect(mesh.has("Jool")).toBe(false);
    expect(mesh.size).toBe(2);
  });

  it("returns empty set when no mesh planets", () => {
    const modes = new Map<string, "icon" | "mesh">([["Kerbin", "icon"]]);
    expect(planetsInMeshModeFromDrawModes(modes).size).toBe(0);
  });
});
