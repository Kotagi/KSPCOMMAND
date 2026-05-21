import { describe, expect, it } from "vitest";
import { buildBodyHierarchy, isMoon, moonsOf } from "./bodyHierarchy";

describe("bodyHierarchy", () => {
  const bodies = [
    { name: "Sun", parentBody: undefined, orbitReferenceBody: undefined },
    { name: "Kerbin", parentBody: "Sun", orbitReferenceBody: "Sun" },
    { name: "Mun", parentBody: "Kerbin", orbitReferenceBody: "Kerbin" },
    { name: "Duna", parentBody: "Sun", orbitReferenceBody: "Sun" },
    { name: "Ike", parentBody: "Duna", orbitReferenceBody: "Duna" },
  ];

  const hierarchy = buildBodyHierarchy(bodies, "Sun");

  it("classifies planets and moons", () => {
    expect(hierarchy.planetNames).toEqual(["Duna", "Kerbin"]);
    expect(moonsOf(hierarchy, "Kerbin")).toEqual(["Mun"]);
    expect(moonsOf(hierarchy, "Duna")).toEqual(["Ike"]);
    expect(isMoon(hierarchy, "Ike")).toBe(true);
    expect(isMoon(hierarchy, "Kerbin")).toBe(false);
  });

  it("maps moon to host planet", () => {
    expect(hierarchy.planetForBody.Ike).toBe("Duna");
    expect(hierarchy.planetForBody.Kerbin).toBe("Kerbin");
  });
});
