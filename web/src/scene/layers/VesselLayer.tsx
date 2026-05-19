import { useMemo, useRef } from "react";
import { Line } from "@react-three/drei";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";

export function VesselLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const cameraMode = useViewStore((s) => s.cameraMode);
  const vesselTarget = useViewStore((s) => s.vesselTargetPosition);
  const meshRef = useRef<THREE.Mesh>(null);
  const displayPos = useRef(new THREE.Vector3());

  const focus = useMemo(() => {
    if (!model) {
      return null;
    }
    if (focusBodyName) {
      return getFocusPosition(model.bodies, focusBodyName);
    }
    if (cameraMode === "currentReferenceBody" && model.referenceBody) {
      return getFocusPosition(model.bodies, model.referenceBody);
    }
    return null;
  }, [model, focusBodyName, cameraMode]);

  const targetScene = useMemo(() => {
    if (!vesselTarget) {
      return null;
    }
    const [x, y, z] = applyWorldShift(vesselTarget, focus, displayScale);
    return new THREE.Vector3(x, y, z);
  }, [vesselTarget, focus, displayScale]);

  const pathPoints = useMemo(() => {
    if (!model?.vesselPathPoints.length) {
      return [];
    }
    return model.vesselPathPoints.map((p) => applyWorldShift(p, focus, displayScale));
  }, [model, focus, displayScale]);

  useFrame((_, delta) => {
    if (!targetScene || !meshRef.current) {
      return;
    }
    if (displayPos.current.lengthSq() === 0) {
      displayPos.current.copy(targetScene);
    }
    const t = Math.min(delta * 4, 1);
    displayPos.current.lerp(targetScene, t);
    meshRef.current.position.copy(displayPos.current);
  });

  if (!model?.canDraw || !targetScene) {
    return null;
  }

  return (
    <group>
      {pathPoints.length >= 2 && (
        <Line
          points={pathPoints}
          color="#61d394"
          lineWidth={1.5}
          dashed
          dashSize={0.3}
          gapSize={0.2}
        />
      )}
      <mesh ref={meshRef} position={targetScene}>
        <coneGeometry args={[0.25, 0.6, 8]} />
        <meshStandardMaterial
          color="#61d394"
          emissive="#61d394"
          emissiveIntensity={0.4}
        />
      </mesh>
    </group>
  );
}
