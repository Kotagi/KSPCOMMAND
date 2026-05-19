import { useEffect, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { useViewStore } from "../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../coords/worldShift";

export function CameraRig() {
  const controlsRef = useRef<React.ComponentRef<typeof OrbitControls>>(null);
  const model = useViewStore((s) => s.model);
  const cameraMode = useViewStore((s) => s.cameraMode);
  const displayScale = useViewStore((s) => s.displayScale);
  const userInteracted = useViewStore((s) => s.userInteractedCamera);
  const setUserInteracted = useViewStore((s) => s.setUserInteractedCamera);
  const { camera } = useThree();
  const lastMode = useRef(cameraMode);

  useEffect(() => {
    if (userInteracted && lastMode.current === cameraMode) {
      return;
    }
    lastMode.current = cameraMode;
    if (!model?.canDraw || !controlsRef.current) {
      return;
    }

    let targetName: string | null = null;
    if (cameraMode === "activeVessel") {
      const vesselName = model.telemetry?.activeVessel?.name;
      if (model.vesselPosition) {
        const focus = getFocusPosition(model.bodies, model.telemetry?.rootBody ?? null);
        const [x, y, z] = applyWorldShift(model.vesselPosition, focus, displayScale);
        controlsRef.current.target.set(x, y, z);
        camera.position.set(x + 8, y + 6, z + 8);
        controlsRef.current.update();
        return;
      }
      void vesselName;
    } else if (cameraMode === "currentReferenceBody" && model.referenceBody) {
      targetName = model.referenceBody;
    } else if (cameraMode === "encounterBody" && model.encounterBody) {
      targetName = model.encounterBody;
    } else if (cameraMode === "route" && model.routeAnchors.length > 0) {
      const mid = model.routeAnchors[Math.floor(model.routeAnchors.length / 2)];
      const focus = getFocusPosition(model.bodies, model.telemetry?.rootBody ?? null);
      const [x, y, z] = applyWorldShift(mid.position, focus, displayScale);
      controlsRef.current.target.set(x, y, z);
      camera.position.set(x + 15, y + 12, z + 15);
      controlsRef.current.update();
      return;
    }

    if (targetName) {
      const body = model.bodies.find((b) => b.body.name === targetName);
      if (body) {
        const focus = getFocusPosition(model.bodies, model.telemetry?.rootBody ?? null);
        const [x, y, z] = applyWorldShift(body.position, focus, displayScale);
        controlsRef.current.target.set(x, y, z);
        const dist = targetName === "Sun" ? 40 : 12;
        camera.position.set(x + dist, y + dist * 0.6, z + dist);
        controlsRef.current.update();
      }
    } else if (cameraMode === "fullSystem") {
      controlsRef.current.target.set(0, 0, 0);
      camera.position.set(25, 18, 25);
      controlsRef.current.update();
    }
  }, [cameraMode, model, displayScale, userInteracted, camera]);

  useFrame(() => {
    camera.far = 100000;
    camera.near = 0.01;
    camera.updateProjectionMatrix();
  });

  return (
    <OrbitControls
      ref={controlsRef}
      makeDefault
      enableDamping
      dampingFactor={0.08}
      onStart={() => setUserInteracted(true)}
    />
  );
}
