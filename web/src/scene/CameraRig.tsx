import { useEffect, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { useViewStore } from "../store/viewStore";
import {
  getBoundsCenterAndRadius,
  getSolarCameraBounds3D,
} from "../camera/solarCameraBounds";

export function CameraRig() {
  const controlsRef = useRef<React.ComponentRef<typeof OrbitControls>>(null);
  const model = useViewStore((s) => s.model);
  const telemetry = useViewStore((s) => s.telemetry);
  const cameraMode = useViewStore((s) => s.cameraMode);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const userInteracted = useViewStore((s) => s.userInteractedCamera);
  const cameraFitNonce = useViewStore((s) => s.cameraFitNonce);
  const setUserInteracted = useViewStore((s) => s.setUserInteractedCamera);
  const { camera } = useThree();
  const lastFitNonce = useRef(-1);

  useEffect(() => {
    if (userInteracted && lastFitNonce.current === cameraFitNonce) {
      return;
    }
    if (!model?.canDraw || !controlsRef.current) {
      return;
    }
    lastFitNonce.current = cameraFitNonce;

    const bounds = getSolarCameraBounds3D(
      model,
      telemetry,
      cameraMode,
      displayScale,
      focusBodyName,
    );
    const { center, radius } = getBoundsCenterAndRadius(bounds);
    const dist = Math.max(radius * 2.8, 4);
    controlsRef.current.target.set(center[0], center[1], center[2]);
    camera.position.set(
      center[0] + dist,
      center[1] + dist * 0.65,
      center[2] + dist,
    );
    controlsRef.current.update();
  }, [
    cameraMode,
    model,
    telemetry,
    displayScale,
    focusBodyName,
    userInteracted,
    cameraFitNonce,
    camera,
    userInteracted,
  ]);

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
