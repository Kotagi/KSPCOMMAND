import { describe, expect, it } from "vitest";
import { buildBodyHierarchy } from "../model/bodyHierarchy";
import { resolveMoonVisibility } from "./moonVisibility";

describe("moonVisibility", () => {
  const bodies = [
    { name: "Sun", parentBody: undefined, orbitReferenceBody: undefined, radiusMeters: 696000000, sphereOfInfluenceMeters: 0 },
    { name: "Kerbin", parentBody: "Sun", orbitReferenceBody: "Sun", radiusMeters: 600000, sphereOfInfluenceMeters: 8.4e7 },
    { name: "Mun", parentBody: "Kerbin", orbitReferenceBody: "Kerbin", radiusMeters: 200000, sphereOfInfluenceMeters: 2.4e6 },
    { name: "Duna", parentBody: "Sun", orbitReferenceBody: "Sun", radiusMeters: 320000, sphereOfInfluenceMeters: 4.7e7 },
    { name: "Ike", parentBody: "Duna", orbitReferenceBody: "Duna", radiusMeters: 130000, sphereOfInfluenceMeters: 1e6 },
  ];

  const hierarchy = buildBodyHierarchy(bodies, "Sun");
  const bodyModels = bodies.map((b) => ({
    body: b,
    position: {
      x: b.name === "Kerbin" ? 1e11 : b.name === "Mun" ? 1e11 + 8e7 : b.name === "Duna" ? 2e11 : b.name === "Ike" ? 2e11 + 3e6 : 0,
      y: 0,
      z: 0,
    },
  }));

  const bodySoiMeters: Record<string, number> = {
    Kerbin: 8.4e7,
    Duna: 4.7e7,
    Mun: 2.4e6,
    Ike: 1e6,
  };

  it("hides moons at solar zoom", () => {
    const result = resolveMoonVisibility({
      hierarchy,
      bodies: bodyModels,
      bodySoiMeters,
      displayScale: 1e-9,
      focus: null,
      cameraPosition: [500, 500, 500],
      cameraMode: "fullSystem",
      focusBodyName: null,
      vesselReferenceBody: null,
      hoverBodyName: null,
      selectedBodyName: null,
      previousHostPlanet: null,
    });
    expect(result.reason).toBe("solar");
    expect(result.visibleBodyNames.has("Mun")).toBe(false);
    expect(result.visibleBodyNames.has("Ike")).toBe(false);
    expect(result.visibleBodyNames.has("Kerbin")).toBe(true);
  });

  it("shows host moons on body focus", () => {
    const result = resolveMoonVisibility({
      hierarchy,
      bodies: bodyModels,
      bodySoiMeters,
      displayScale: 1e-9,
      focus: bodyModels.find((b) => b.body.name === "Duna")!.position,
      cameraPosition: [0, 0, 5],
      cameraMode: "bodyFocus",
      focusBodyName: "Duna",
      vesselReferenceBody: null,
      hoverBodyName: null,
      selectedBodyName: null,
      previousHostPlanet: null,
    });
    expect(result.activeHostPlanet).toBe("Duna");
    expect(result.visibleBodyNames.has("Ike")).toBe(true);
    expect(result.visibleBodyNames.has("Mun")).toBe(false);
    expect(result.visibleBodyNames.has("Moho")).toBe(false);
  });

  it("shows Kerbin moons when vessel orbits Mun", () => {
    const result = resolveMoonVisibility({
      hierarchy,
      bodies: bodyModels,
      bodySoiMeters,
      displayScale: 1e-9,
      focus: null,
      cameraPosition: [500, 500, 500],
      cameraMode: "activeVessel",
      focusBodyName: null,
      vesselReferenceBody: "Mun",
      hoverBodyName: null,
      selectedBodyName: null,
      previousHostPlanet: null,
    });
    expect(result.activeHostPlanet).toBe("Kerbin");
    expect(result.visibleBodyNames.has("Mun")).toBe(true);
    expect(result.visibleBodyNames.has("Minmus")).toBe(false);
  });
});
