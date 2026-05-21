import { useMapV2 } from "../../../map-v2/MapV2Context";
import { useV2RootSegments, useV2SceneTrails } from "../../../map-v2/useMapV2Trails";
import { OrbitTrailV2 } from "./OrbitTrailV2";

export function PlanetOrbitLayer() {
  const { mapContext, sceneFrame, layers } = useMapV2();
  const rootSegs = useV2RootSegments(
    mapContext,
    "BodyOrbit",
    { planetOnly: true },
    layers.planetOrbits,
  );
  const trails = useV2SceneTrails(rootSegs, sceneFrame);

  if (!layers.planetOrbits || trails.length === 0) {
    return null;
  }

  return (
    <group>
      {trails.map((t) => (
        <OrbitTrailV2
          key={t.key}
          lineKey={t.key}
          bodyName={t.bodyName}
          points={t.points}
          anchorIndex={t.anchorIndex}
          closedWithDuplicateEndpoint={t.closed}
        />
      ))}
    </group>
  );
}
