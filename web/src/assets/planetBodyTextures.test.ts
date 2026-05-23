import { afterEach, describe, expect, it } from "vitest";
import {
  buildBodyTextureCacheKey,
  clearBodyTextureCacheForTests,
  resolveBodyTextureUrl,
} from "./planetBodyTextures";

describe("planetBodyTextures", () => {
  afterEach(() => {
    clearBodyTextureCacheForTests();
  });

  it("resolveBodyTextureUrl keeps rev query on relative paths", () => {
    const url = resolveBodyTextureUrl("/assets/bodies/Kerbin.jpg?rev=abc12345");
    expect(url).toContain("/assets/bodies/Kerbin.jpg?rev=abc12345");
  });

  it("buildBodyTextureCacheKey changes when revision changes", () => {
    const base = "/assets/bodies/Kerbin.jpg";
    expect(buildBodyTextureCacheKey(base, "a")).not.toBe(buildBodyTextureCacheKey(base, "b"));
  });
});
