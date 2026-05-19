import { useMemo } from "react";
import * as THREE from "three";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";
import type { Vector3 } from "../../telemetry/schema-v6";
import { getKerbinTexture, getSunTexture } from "../../assets/proceduralTextures";

const BODY_COLORS: Record<string, string> = {
  Sun: "#ffd166",
  Kerbin: "#4ecdc4",
  Mun: "#9db1c3",
  Minmus: "#c792ea",
  Duna: "#ff6b6b",
  Eve: "#a29bfe",
  Gilly: "#dfe6e9",
  Moho: "#fab1a0",
  Dres: "#b2bec3",
  Jool: "#74b9ff",
  Laythe: "#55efc4",
  Vall: "#81ecec",
  Tylo: "#636e72",
  Bop: "#ffeaa7",
  Pol: "#fdcb6e",
  Eeloo: "#dfe6e9",
};

function bodyColor(name: string | undefined): string {
  return (name && BODY_COLORS[name]) || "#67d3ff";
}

function bodyMap(name: string): THREE.Texture | null {
  if (name === "Sun") {
    return getSunTexture();
  }
  if (name === "Kerbin") {
    return getKerbinTexture();
  }
  return null;
}

export function BodiesLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const cameraMode = useViewStore((s) => s.cameraMode);

  const focus = useMemo((): Vector3 | null => {
    if (!model) {
      return null;
    }
    if (focusBodyName) {
      return getFocusPosition(model.bodies, focusBodyName);
    }
    if (cameraMode === "currentReferenceBody" && model.referenceBody) {
      return getFocusPosition(model.bodies, model.referenceBody);
    }
    if (cameraMode === "encounterBody" && model.encounterBody) {
      return getFocusPosition(model.bodies, model.encounterBody);
    }
    return null;
  }, [model, focusBodyName, cameraMode]);

  if (!model?.canDraw) {
    return null;
  }

  return (
    <group>
      {model.bodies.map((entry) => {
        const name = entry.body.name ?? "body";
        const radius = Math.max(entry.body.radiusMeters ?? 1000, 1000);
        const visualRadius = Math.max(radius * displayScale, name === "Sun" ? 2 : 0.15);
        const [x, y, z] = applyWorldShift(entry.position, focus, displayScale);
        const isSun = name === "Sun" || name === model.telemetry?.rootBody;
        const map = bodyMap(name);
        return (
          <mesh key={name} position={[x, y, z]} renderOrder={isSun ? 10 : 1}>
            <sphereGeometry args={[visualRadius, 24, 24]} />
            <meshStandardMaterial
              color={bodyColor(name)}
              map={map ?? undefined}
              emissive={isSun ? new THREE.Color("#ffaa00") : new THREE.Color("#000000")}
              emissiveIntensity={isSun ? 1.2 : 0}
            />
          </mesh>
        );
      })}
    </group>
  );
}
