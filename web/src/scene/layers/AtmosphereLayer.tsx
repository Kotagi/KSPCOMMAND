import { useMemo } from "react";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";

export function AtmosphereLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const cameraMode = useViewStore((s) => s.cameraMode);

  const focus = useMemo(() => {
    if (!model) {
      return null;
    }
    if (focusBodyName) {
      return getFocusPosition(model.bodies, focusBodyName);
    }
    if (cameraMode === "currentReferenceBody" && model.referenceBody) {
      return getFocusPosition(model.bodies, model.referenceBody);
    }
    return null;
  }, [model, focusBodyName, cameraMode]);

  if (!model?.canDraw) {
    return null;
  }

  return (
    <group>
      {model.bodies.map((entry) => {
        if (!entry.body.hasAtmosphere) {
          return null;
        }
        const name = entry.body.name ?? "body";
        const radius = Math.max(entry.body.radiusMeters ?? 1000, 1000);
        const depth = entry.body.atmosphereDepthMeters ?? radius * 0.05;
        const visualRadius = (radius + depth) * displayScale;
        const [x, y, z] = applyWorldShift(entry.position, focus, displayScale);
        return (
          <mesh key={`atmo-${name}`} position={[x, y, z]}>
            <sphereGeometry args={[visualRadius, 24, 24]} />
            <meshBasicMaterial
              color="#67d3ff"
              transparent
              opacity={0.08}
              depthWrite={false}
            />
          </mesh>
        );
      })}
    </group>
  );
}
