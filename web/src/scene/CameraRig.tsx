import { useEffect, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { MOUSE } from "three";
import { useViewStore } from "../store/viewStore";
import {
  getBoundsCenterAndRadius,
  getEclipticLevelCameraPose,
  getSolarCameraBounds3D,
} from "../camera/solarCameraBounds";
import { useMoonVisibilityContext } from "./MoonVisibilityContext";
import { buildMapContext } from "../map-v2/MapContext";
import { getStarMarkerCameraBounds } from "../map-v3/camera/starCameraBounds";
import { composeMapV3Layers } from "../map-v3/MapComposer";
import { MAP_V3_LAYERS_PHASE3 } from "../map-v3/layerFlags";

function isMapV3StarOnlyView(
  solarRenderMode: string,
  activeLayerIds: string[],
): boolean {
  return (
    solarRenderMode === "3d-v3" &&
    activeLayerIds.length === 1 &&
    activeLayerIds[0] === "StarMarkerLayer"
  );
}

export function CameraRig() {
  const controlsRef = useRef<React.ComponentRef<typeof OrbitControls>>(null);
  const model = useViewStore((s) => s.model);
  const telemetry = useViewStore((s) => s.telemetry);
  const solarRenderMode = useViewStore((s) => s.solarRenderMode);
  const cameraMode = useViewStore((s) => s.cameraMode);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const { displayFocus, reason: moonLodReason } = useMoonVisibilityContext();
  const userInteracted = useViewStore((s) => s.userInteractedCamera);
  const cameraFitNonce = useViewStore((s) => s.cameraFitNonce);
  const setUserInteracted = useViewStore((s) => s.setUserInteractedCamera);
  const { camera } = useThree();
  const lastFitNonce = useRef(-1);

  useEffect(() => {
    if (userInteracted && lastFitNonce.current === cameraFitNonce) {
      return;
    }
    if (!controlsRef.current) {
      return;
    }

    const v3StarOnly = isMapV3StarOnlyView(
      solarRenderMode,
      composeMapV3Layers(MAP_V3_LAYERS_PHASE3),
    );

    if (v3StarOnly) {
      if (!buildMapContext(telemetry)?.canDraw) {
        return;
      }
    } else if (!model?.canDraw) {
      return;
    }

    lastFitNonce.current = cameraFitNonce;
    const bounds = v3StarOnly
      ? getStarMarkerCameraBounds(buildMapContext(telemetry), displayScale)
      : getSolarCameraBounds3D(
          model!,
          telemetry,
          cameraMode,
          displayScale,
          focusBodyName,
          displayFocus,
        );
    const { center, radius } = getBoundsCenterAndRadius(bounds);
    const hostCentered =
      v3StarOnly ||
      cameraMode === "bodyFocus" ||
      moonLodReason === "soiZoom";
    const orbitCenter: [number, number, number] = hostCentered ? [0, 0, 0] : center;
    const { position, up } = getEclipticLevelCameraPose(
      orbitCenter,
      hostCentered ? Math.max(radius, 0.5) : radius,
    );
    controlsRef.current.target.set(orbitCenter[0], orbitCenter[1], orbitCenter[2]);
    camera.position.set(position[0], position[1], position[2]);
    camera.up.set(up[0], up[1], up[2]);
    controlsRef.current.update();
  }, [
    cameraMode,
    model,
    telemetry,
    solarRenderMode,
    displayScale,
    focusBodyName,
    displayFocus,
    moonLodReason,
    userInteracted,
    cameraFitNonce,
    camera,
  ]);

  useFrame(() => {
    camera.far = 100000;
    camera.near = 0.01;
    camera.updateProjectionMatrix();
  });

  const isBodyFocus = cameraMode === "bodyFocus";

  return (
    <OrbitControls
      ref={controlsRef}
      makeDefault
      enableDamping
      dampingFactor={0.08}
      minDistance={isBodyFocus ? 0.4 : 0.01}
      maxDistance={isBodyFocus ? 5000 : 100000}
      mouseButtons={{
        LEFT: MOUSE.PAN,
        MIDDLE: MOUSE.DOLLY,
        RIGHT: MOUSE.ROTATE,
      }}
      onStart={() => setUserInteracted(true)}
    />
  );
}
