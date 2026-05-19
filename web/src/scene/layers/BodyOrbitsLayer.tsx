import { useMemo } from "react";
import { Line } from "@react-three/drei";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";

export function BodyOrbitsLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);

  const focus = useMemo(() => {
    if (!model || !focusBodyName) {
      return null;
    }
    return getFocusPosition(model.bodies, focusBodyName);
  }, [model, focusBodyName]);

  const paths = useMemo(() => {
    return (model?.bodyOrbitPaths ?? [])
      .map((path, index) => {
        const samples = path.samples ?? [];
        if (samples.length < 2) {
          return null;
        }
        const points = samples
          .filter((s) => s.positionRootRelativeMeters)
          .map((s) =>
            applyWorldShift(s.positionRootRelativeMeters!, focus, displayScale),
          );
        if (points.length < 2) {
          return null;
        }
        return { key: `body-orbit-${path.bodyName ?? index}`, points };
      })
      .filter(Boolean) as { key: string; points: [number, number, number][] }[];
  }, [model, focus, displayScale]);

  if (!paths.length) {
    return null;
  }

  return (
    <group>
      {paths.map((path) => (
        <Line
          key={path.key}
          points={path.points}
          color="#9db1c3"
          lineWidth={1}
          transparent
          opacity={0.35}
        />
      ))}
    </group>
  );
}
