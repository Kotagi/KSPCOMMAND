import type { Vector3 } from "../../../telemetry/schema-v6";
import { useMapV3 } from "../../../map-v3/MapV3Context";
import { toScenePoint } from "../../../map-v3/SceneFrame";
import { isPlanetBodyTextureReady } from "../../../map-v3/elements/planetBody/planetBodyTextureFields";
import { bodyMeshRadius } from "../../bodyVisualScale";
import { useKspBodyMapColor } from "../../bodyMapColors";
import { FlatPlanetBody } from "./FlatPlanetBody";
import { PlanetBodyDot } from "./PlanetBodyDot";
import { TexturedPlanetBody } from "./TexturedPlanetBody";
import { usePlanetBodyDrawMode } from "./usePlanetBodyDrawMode";
import { useViewStore } from "../../../store/viewStore";

export function PlanetBodyMesh({
  bodyName,
  radiusMeters,
  rootPosition,
  bodyTextureUrl,
  bodyTextureRevision,
  bodyTextureStatus,
}: {
  bodyName: string;
  radiusMeters: number;
  rootPosition: Vector3;
  bodyTextureUrl?: string;
  bodyTextureRevision?: string;
  bodyTextureStatus?: string;
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

  if (
    isPlanetBodyTextureReady({
      bodyTextureUrl,
      bodyTextureRevision,
      bodyTextureStatus,
    })
  ) {
    return (
      <TexturedPlanetBody
        radius={meshR}
        position={scenePosition}
        renderOrder={1}
        fallbackColor={color}
        textureUrl={bodyTextureUrl}
        textureRevision={bodyTextureRevision}
      />
    );
  }

  return (
    <FlatPlanetBody
      radius={meshR}
      position={scenePosition}
      color={color}
      renderOrder={1}
    />
  );
}
