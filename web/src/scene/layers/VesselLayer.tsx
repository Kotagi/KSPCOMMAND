import { useMemo } from "react";
import { Line } from "@react-three/drei";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";

export function VesselLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);

  const focus = useMemo(() => {
    if (!model || !focusBodyName) {
      return null;
    }
    return getFocusPosition(model.bodies, focusBodyName);
  }, [model, focusBodyName]);

  const vesselPos = useMemo(() => {
    if (!model?.vesselPosition) {
      return null;
    }
    return applyWorldShift(model.vesselPosition, focus, displayScale);
  }, [model, focus, displayScale]);

  const pathPoints = useMemo(() => {
    if (!model?.vesselPathPoints.length) {
      return [];
    }
    return model.vesselPathPoints.map((p) => applyWorldShift(p, focus, displayScale));
  }, [model, focus, displayScale]);

  if (!model?.canDraw) {
    return null;
  }

  return (
    <group>
      {pathPoints.length >= 2 && (
        <Line points={pathPoints} color="#61d394" lineWidth={1.5} dashed dashSize={0.3} gapSize={0.2} />
      )}
      {vesselPos && (
        <mesh position={vesselPos}>
          <coneGeometry args={[0.25, 0.6, 8]} />
          <meshStandardMaterial color="#61d394" emissive="#61d394" emissiveIntensity={0.4} />
        </mesh>
      )}
    </group>
  );
}
