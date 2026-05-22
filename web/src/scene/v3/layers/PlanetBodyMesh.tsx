import * as THREE from "three";
import { useThree } from "@react-three/fiber";
import type { Vector3 } from "../../../telemetry/schema-v6";
import { useMapV3 } from "../../../map-v3/MapV3Context";
import { toScenePoint } from "../../../map-v3/SceneFrame";
import {
  PLANET_BODY_ICON_SPHERE_SEGMENTS,
  PLANET_BODY_MESH_SPHERE_SEGMENTS,
  planetBodyIconRadius,
  resolvePlanetBodyDrawMode,
} from "../../../map-v3/elements/planetBody/planetBodyLod";
import { bodyMeshRadius } from "../../bodyVisualScale";
import { useKspBodyMapColor } from "../../bodyMapColors";

export function PlanetBodyMesh({
  bodyName,
  radiusMeters,
  rootPosition,
}: {
  bodyName: string;
  radiusMeters: number;
  rootPosition: Vector3;
}) {
  const { mapContext, sceneFrame, hostPlanetOpen } = useMapV3();
  const { camera } = useThree();
  const color = useKspBodyMapColor(bodyName);

  if (!mapContext) {
    return null;
  }

  const meshR = bodyMeshRadius({
    bodyName,
    radiusMeters,
    displayScale: sceneFrame.displayScale,
    hierarchy: mapContext.hierarchy,
    hostPlanetOpen,
  });

  const [x, y, z] = toScenePoint(rootPosition, sceneFrame);
  const camDist = camera.position.distanceTo(new THREE.Vector3(x, y, z));
  const drawMode = resolvePlanetBodyDrawMode({
    sceneMeshRadius: meshR,
    cameraDistance: camDist,
  });

  if (drawMode === "icon") {
    const iconR = planetBodyIconRadius();
    return (
      <mesh position={[x, y, z]} renderOrder={2}>
        <sphereGeometry
          args={[iconR, PLANET_BODY_ICON_SPHERE_SEGMENTS, PLANET_BODY_ICON_SPHERE_SEGMENTS]}
        />
        <meshBasicMaterial color={color} />
      </mesh>
    );
  }

  return (
    <mesh position={[x, y, z]} renderOrder={1}>
      <sphereGeometry
        args={[meshR, PLANET_BODY_MESH_SPHERE_SEGMENTS, PLANET_BODY_MESH_SPHERE_SEGMENTS]}
      />
      <meshBasicMaterial color={color} />
    </mesh>
  );
}
