import * as THREE from "three";

const textureCache = new Map<string, THREE.Texture>();
const loader = new THREE.TextureLoader();

export function resolveBodyTextureUrl(urlPath: string): string {
  if (urlPath.startsWith("http://") || urlPath.startsWith("https://")) {
    return urlPath;
  }

  if (typeof window !== "undefined" && window.location?.origin) {
    const path = urlPath.startsWith("/") ? urlPath.slice(1) : urlPath;
    return new URL(path, `${window.location.origin}/`).href;
  }

  return urlPath.startsWith("/") ? urlPath : `/${urlPath}`;
}

export function buildBodyTextureCacheKey(url: string, revision?: string): string {
  return `${url}::${revision ?? ""}`;
}

function configureBodyTexture(texture: THREE.Texture): void {
  // flipY true: Three SphereGeometry v=1 at +Y must match KSP albedo north in JPEG.
  texture.flipY = true;
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.needsUpdate = true;
}

export function isBodyTextureReady(texture: THREE.Texture | null | undefined): boolean {
  const image = texture?.image as { width?: number; height?: number } | undefined;
  return !!image && (image.width ?? 0) > 0 && (image.height ?? 0) > 0;
}

export function loadBodyTexture(
  urlPath: string | undefined,
  revision: string | undefined,
  onLoad: (texture: THREE.Texture) => void,
  onError?: (resolvedUrl: string) => void,
): void {
  if (!urlPath) {
    return;
  }

  const resolvedUrl = resolveBodyTextureUrl(urlPath);
  const cacheKey = buildBodyTextureCacheKey(resolvedUrl, revision);
  const cached = textureCache.get(cacheKey);

  if (cached && isBodyTextureReady(cached)) {
    onLoad(cached);
    return;
  }

  loader.load(
    resolvedUrl,
    (texture) => {
      if (!isBodyTextureReady(texture)) {
        console.warn("[KspWebMap] Body texture has no image data:", resolvedUrl);
        onError?.(resolvedUrl);
        return;
      }
      configureBodyTexture(texture);
      textureCache.set(cacheKey, texture);
      onLoad(texture);
    },
    undefined,
    () => {
      console.warn("[KspWebMap] Body texture failed to load:", resolvedUrl);
      onError?.(resolvedUrl);
    },
  );
}

/** Test helper — clears module cache between Vitest cases. */
export function clearBodyTextureCacheForTests(): void {
  textureCache.forEach((texture) => texture.dispose());
  textureCache.clear();
}
