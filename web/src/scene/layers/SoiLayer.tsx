import { useMemo } from "react";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";

export function SoiLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const showSoi = useViewStore((s) => s.telemetry) !== null;

  const focus = useMemo(() => {
    if (!model || !focusBodyName) {
      return null;
    }
    return getFocusPosition(model.bodies, focusBodyName);
  }, [model, focusBodyName]);

  if (!showSoi || !model?.canDraw) {
    return null;
  }

  return (
    <group>
      {model.bodies.map((entry) => {
        const soi = entry.body.sphereOfInfluenceMeters;
        if (!soi || soi <= 0) {
          return null;
        }
        const name = entry.body.name ?? "body";
        const visualRadius = soi * displayScale;
        if (visualRadius > 5000) {
          return null;
        }
        const [x, y, z] = applyWorldShift(entry.position, focus, displayScale);
        return (
          <mesh key={`soi-${name}`} position={[x, y, z]}>
            <sphereGeometry args={[visualRadius, 32, 32]} />
            <meshBasicMaterial
              color="#67d3ff"
              transparent
              opacity={0.06}
              wireframe={false}
              depthWrite={false}
            />
          </mesh>
        );
      })}
    </group>
  );
}
