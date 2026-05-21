import { describe, expect, it } from "vitest";
import { buildBodyHierarchy } from "../model/bodyHierarchy";
import { bodyMeshRadius } from "./bodyVisualScale";

describe("bodyVisualScale", () => {
  const hierarchy = buildBodyHierarchy(
    [
      { name: "Sun", parentBody: undefined },
      { name: "Duna", parentBody: "Sun" },
      { name: "Ike", parentBody: "Duna" },
    ],
    "Sun",
  );

  it("uses physical radius for moons without solar floor", () => {
    const ikeR = bodyMeshRadius({
      bodyName: "Ike",
      radiusMeters: 130000,
      displayScale: 1e-9,
      hierarchy,
      hostPlanetOpen: true,
    });
    expect(ikeR).toBeCloseTo(0.00013, 6);
    expect(ikeR).toBeLessThan(0.05);
  });

  it("applies small floor to planets at solar zoom only", () => {
    const dunaR = bodyMeshRadius({
      bodyName: "Duna",
      radiusMeters: 320000,
      displayScale: 1e-9,
      hierarchy,
      hostPlanetOpen: false,
    });
    expect(dunaR).toBeGreaterThanOrEqual(0.05);
  });
});
