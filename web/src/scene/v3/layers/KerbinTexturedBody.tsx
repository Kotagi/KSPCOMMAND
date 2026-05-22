import { useEffect, useState } from "react";
import * as THREE from "three";
import { loadKerbinBodyTexture, resolveKerbinBodyTextureUrl } from "../../../assets/planetBodyTextures";
import { PLANET_BODY_MESH_SPHERE_SEGMENTS } from "../../../map-v3/elements/planetBody/planetBodyLod";

/** Kerbin mesh-mode body only (icon mode uses PlanetBodyDot). */
export function KerbinTexturedBody({
  radius,
  position,
  renderOrder,
  fallbackColor,
}: {
  radius: number;
  position: [number, number, number];
  renderOrder: number;
  fallbackColor: string;
}) {
  const [map, setMap] = useState<THREE.Texture | null>(null);

  useEffect(() => {
    let disposed = false;
    loadKerbinBodyTexture(
      (texture) => {
        if (!disposed) {
          setMap(texture);
        }
      },
      (url) => {
        if (!disposed) {
          console.warn("[KspWebMap] Kerbin texture URL:", url);
        }
      },
    );
    return () => {
      disposed = true;
    };
  }, []);

  return (
    <mesh position={position} renderOrder={renderOrder}>
      <sphereGeometry
        args={[radius, PLANET_BODY_MESH_SPHERE_SEGMENTS, PLANET_BODY_MESH_SPHERE_SEGMENTS]}
      />
      {map ? (
        <meshBasicMaterial map={map} />
      ) : (
        <meshBasicMaterial color={fallbackColor} />
      )}
    </mesh>
  );
}

/** Dev-only: log resolved URL once per session. */
export function useKerbinTextureUrlDebug(): void {
  useEffect(() => {
    console.log("[KspWebMap] Kerbin texture:", resolveKerbinBodyTextureUrl());
  }, []);
}
