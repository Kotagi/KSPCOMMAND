import * as THREE from "three";
import { afterEach, describe, expect, it } from "vitest";
import {
  applyBodyTextureDisplaySettings,
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

  it("applyBodyTextureDisplaySettings flips Y only (longitude fixed at DLL export)", () => {
    const texture = new THREE.Texture();
    applyBodyTextureDisplaySettings(texture);
    expect(texture.flipY).toBe(true);
    expect(texture.wrapS).toBe(THREE.ClampToEdgeWrapping);
    expect(texture.repeat.x).toBe(1);
    expect(texture.offset.x).toBe(0);
    expect(texture.colorSpace).toBe(THREE.SRGBColorSpace);
  });
});
