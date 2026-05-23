import { describe, expect, it } from "vitest";
import {
  isPlanetBodyTextureReady,
  planetBodyTextureFields,
} from "./planetBodyTextureFields";

describe("planetBodyTextureFields", () => {
  it("isPlanetBodyTextureReady requires ready status and url", () => {
    expect(
      isPlanetBodyTextureReady({
        bodyTextureStatus: "ready",
        bodyTextureUrl: "/assets/bodies/Kerbin.jpg",
      }),
    ).toBe(true);
    expect(
      isPlanetBodyTextureReady({
        bodyTextureStatus: "pending",
        bodyTextureUrl: "/assets/bodies/Kerbin.jpg",
      }),
    ).toBe(false);
  });

  it("planetBodyTextureFields copies telemetry fields", () => {
    expect(
      planetBodyTextureFields({
        name: "Kerbin",
        bodyTextureUrl: "/assets/bodies/Kerbin.jpg",
        bodyTextureRevision: "abc",
        bodyTextureStatus: "ready",
      }),
    ).toEqual({
      bodyTextureUrl: "/assets/bodies/Kerbin.jpg",
      bodyTextureRevision: "abc",
      bodyTextureStatus: "ready",
    });
  });
});
