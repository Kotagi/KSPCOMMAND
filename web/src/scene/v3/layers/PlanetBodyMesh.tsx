import { useMemo, type ReactNode } from "react";
import type { Vector3 } from "../../../telemetry/schema-v6";
import { useMapV3 } from "../../../map-v3/MapV3Context";
import { toScenePoint } from "../../../map-v3/SceneFrame";
import { isPlanetBodyTextureReady } from "../../../map-v3/elements/planetBody/planetBodyTextureFields";
import {
  resolvePlanetBodyOrientationAtUt,
  type CelestialBodyWithOrientation,
} from "../../../map-v3/elements/planetBody/planetBodyOrientationFields";
import { KSP_ROOT_IDENTITY_QUATERNION } from "../../../coords/kspBodyOrientation";
import { bodyMeshRadius } from "../../bodyVisualScale";
import { useKspBodyMapColor } from "../../bodyMapColors";
import { FlatPlanetBody } from "./FlatPlanetBody";
import { PlanetBodyDot } from "./PlanetBodyDot";
import { PlanetBodyOrientedGroup } from "./PlanetBodyOrientedGroup";
import { PlanetBodyMeshPoleFrame } from "./PlanetBodyMeshPoleFrame";
import { PlanetBodySpinAxisLine } from "./PlanetBodySpinAxisLine";
import { TexturedPlanetBody } from "./TexturedPlanetBody";
import { usePlanetBodyDrawMode } from "./usePlanetBodyDrawMode";
import { useViewStore } from "../../../store/viewStore";

const MESH_ORIGIN: [number, number, number] = [0, 0, 0];

function PlanetBodyMeshOriented({
  scenePosition,
  meshR,
  color,
  body,
  gameUniversalTimeSeconds,
  showSpinAxis,
  bodyTextureUrl,
  bodyTextureRevision,
  bodyTextureStatus,
}: {
  scenePosition: [number, number, number];
  meshR: number;
  color: string;
  body: CelestialBodyWithOrientation | undefined;
  gameUniversalTimeSeconds: number;
  showSpinAxis: boolean;
  bodyTextureUrl?: string;
  bodyTextureRevision?: string;
  bodyTextureStatus?: string;
}) {
  const orientationKsp = useMemo(
    () =>
      resolvePlanetBodyOrientationAtUt(body, gameUniversalTimeSeconds) ??
      KSP_ROOT_IDENTITY_QUATERNION,
    [
      body,
      gameUniversalTimeSeconds,
      body?.bodyOrientationSampleUniversalTimeSeconds,
      body?.bodyOrientationRootRelative?.x,
      body?.bodyOrientationRootRelative?.y,
      body?.bodyOrientationRootRelative?.z,
      body?.bodyOrientationRootRelative?.w,
      body?.angularVelocityRootRelativeRadPerSec?.x,
      body?.angularVelocityRootRelativeRadPerSec?.y,
      body?.angularVelocityRootRelativeRadPerSec?.z,
    ],
  );

  let meshChild: ReactNode;
  if (
    isPlanetBodyTextureReady({
      bodyTextureUrl,
      bodyTextureRevision,
      bodyTextureStatus,
    })
  ) {
    meshChild = (
      <TexturedPlanetBody
        radius={meshR}
        position={MESH_ORIGIN}
        renderOrder={1}
        fallbackColor={color}
        textureUrl={bodyTextureUrl}
        textureRevision={bodyTextureRevision}
      />
    );
  } else {
    meshChild = (
      <FlatPlanetBody
        radius={meshR}
        position={MESH_ORIGIN}
        color={color}
        renderOrder={1}
      />
    );
  }

  return (
    <group position={scenePosition}>
      <PlanetBodyOrientedGroup orientationKsp={orientationKsp}>
        {showSpinAxis ? <PlanetBodySpinAxisLine radius={meshR} /> : null}
        <PlanetBodyMeshPoleFrame>{meshChild}</PlanetBodyMeshPoleFrame>
      </PlanetBodyOrientedGroup>
    </group>
  );
}

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
  const devPlanetBodySpinAxisVisible = useViewStore(
    (s) => s.devPlanetBodySpinAxisVisible,
  );
  const gameUniversalTimeSeconds = useViewStore(
    (s) => s.telemetry?.gameUniversalTimeSeconds ?? 0,
  );
  const color = useKspBodyMapColor(bodyName);

  if (!mapContext) {
    return null;
  }

  const bodyEntry = mapContext.bodyByName.get(bodyName);
  const body = bodyEntry?.body as CelestialBodyWithOrientation | undefined;

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

  return (
    <PlanetBodyMeshOriented
      scenePosition={scenePosition}
      meshR={meshR}
      color={color}
      body={body}
      gameUniversalTimeSeconds={gameUniversalTimeSeconds}
      showSpinAxis={devPlanetBodySpinAxisVisible}
      bodyTextureUrl={bodyTextureUrl}
      bodyTextureRevision={bodyTextureRevision}
      bodyTextureStatus={bodyTextureStatus}
    />
  );
}
