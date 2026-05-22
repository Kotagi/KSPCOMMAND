import type { Vector3 } from "../../../telemetry/schema-v6";
import { useMapV3 } from "../../../map-v3/MapV3Context";
import { toScenePoint } from "../../../map-v3/SceneFrame";
import { isKerbinBodyName } from "../../../assets/planetBodyTextures";
import { PLANET_BODY_MESH_SPHERE_SEGMENTS } from "../../../map-v3/elements/planetBody/planetBodyLod";
import { bodyMeshRadius } from "../../bodyVisualScale";
import { useKspBodyMapColor } from "../../bodyMapColors";
import { KerbinTexturedBody } from "./KerbinTexturedBody";
import { PlanetBodyDot } from "./PlanetBodyDot";
import { usePlanetBodyDrawMode } from "./usePlanetBodyDrawMode";
import { useViewStore } from "../../../store/viewStore";

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
  const devPlanetBodyLodOverride = useViewStore((s) => s.devPlanetBodyLodOverride);
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

  const scenePosition = toScenePoint(rootPosition, sceneFrame);
  const drawMode = usePlanetBodyDrawMode(
    meshR,
    scenePosition,
    devPlanetBodyLodOverride,
  );

  if (drawMode === "icon") {
    return <PlanetBodyDot position={scenePosition} color={color} />;
  }

  if (isKerbinBodyName(bodyName)) {
    return (
      <KerbinTexturedBody
        radius={meshR}
        position={scenePosition}
        renderOrder={1}
        fallbackColor={color}
      />
    );
  }

  return (
    <mesh position={scenePosition} renderOrder={1}>
      <sphereGeometry
        args={[meshR, PLANET_BODY_MESH_SPHERE_SEGMENTS, PLANET_BODY_MESH_SPHERE_SEGMENTS]}
      />
      <meshBasicMaterial color={color} />
    </mesh>
  );
}
