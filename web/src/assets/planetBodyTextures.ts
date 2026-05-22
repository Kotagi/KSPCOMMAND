import * as THREE from "three";

/** Static file under KspWebMap/Web/assets/ (see build copy from assets/planets/kerbol/). */
export function resolveKerbinBodyTextureUrl(): string {
  if (typeof window !== "undefined" && window.location?.origin) {
    return new URL("assets/kerbin00.png", `${window.location.origin}/`).href;
  }
  return "/assets/kerbin00.png";
}

export function isKerbinBodyName(bodyName: string): boolean {
  return bodyName === "Kerbin";
}

export function loadKerbinBodyTexture(
  onLoad: (texture: THREE.Texture) => void,
  onError?: (url: string) => void,
): void {
  const url = resolveKerbinBodyTextureUrl();
  const loader = new THREE.TextureLoader();
  loader.load(
    url,
    (texture) => {
      texture.colorSpace = THREE.SRGBColorSpace;
      onLoad(texture);
    },
    undefined,
    () => {
      console.warn("[KspWebMap] Kerbin texture failed to load:", url);
      onError?.(url);
    },
  );
}
