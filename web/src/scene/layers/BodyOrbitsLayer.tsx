import { useMemo } from "react";
import { useViewStore } from "../../store/viewStore";
import { DirectionalOrbitTrail } from "./DirectionalOrbitTrail";
import { applyWorldShift } from "../../coords/worldShift";
import { resolveTrailRenderMode } from "../../coords/buildBodyOrbitTrail";
import { useLayerFocus } from "./useLayerFocus";
import { useMoonVisibilityContext } from "../MoonVisibilityContext";
import type { Vector3 } from "../../telemetry/schema-v6";

function rootPointsToLine(
  rootPoints: Vector3[],
  focus: ReturnType<typeof useLayerFocus>,
  displayScale: number,
): [number, number, number][] {
  return rootPoints.map((p) =>
    applyWorldShift(p, focus, displayScale),
  ) as [number, number, number][];
}

export function BodyOrbitsLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focus = useLayerFocus();
  const { visibleBodyNames } = useMoonVisibilityContext();

  const paths = useMemo(() => {
    const lines: {
      key: string;
      bodyName?: string;
      points: [number, number, number][];
      anchorIndex: number;
      closedWithDuplicateEndpoint: boolean;
    }[] = [];

    (model?.bodyOrbitPaths ?? []).forEach((path, pathIndex) => {
      if (!path?.bodyName || !visibleBodyNames.has(path.bodyName)) {
        return;
      }
      if (resolveTrailRenderMode(path) === "hidden") {
        return;
      }
      const samples = path.samples ?? [];
      const points = samples
        .map((s) => s.positionRootRelativeMeters)
        .filter((p): p is Vector3 => p != null);
      if (points.length < 2) {
        return;
      }
      const scenePoints = rootPointsToLine(points, focus, displayScale);
      lines.push({
        key: `body-orbit-${path.bodyName ?? pathIndex}`,
        bodyName: path.bodyName,
        points: scenePoints,
        anchorIndex: 0,
        closedWithDuplicateEndpoint: false,
      });
    });

    return lines;
  }, [model, focus, displayScale, visibleBodyNames]);

  if (!paths.length) {
    return null;
  }

  return (
    <group>
      {paths.map((path) => (
        <DirectionalOrbitTrail
          key={path.key}
          lineKey={path.key}
          bodyName={path.bodyName}
          points={path.points}
          anchorIndex={path.anchorIndex}
          closedWithDuplicateEndpoint={path.closedWithDuplicateEndpoint}
        />
      ))}
    </group>
  );
}
