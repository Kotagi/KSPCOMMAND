import { useMemo } from "react";
import { Html } from "@react-three/drei";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";

const PRIORITY_BODIES = new Set(["Sun", "Kerbin", "Mun", "Minmus", "Duna"]);

interface LabelItem {
  key: string;
  text: string;
  position: [number, number, number];
  priority: number;
}

export function LabelsLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const cameraMode = useViewStore((s) => s.cameraMode);
  const quality = useViewStore((s) => s.getQuality());

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
    if (cameraMode === "encounterBody" && model.encounterBody) {
      return getFocusPosition(model.bodies, model.encounterBody);
    }
    return null;
  }, [model, focusBodyName, cameraMode]);

  const labels = useMemo((): LabelItem[] => {
    if (!model?.canDraw || !quality.labelsEnabled) {
      return [];
    }
    const items: LabelItem[] = [];
    model.bodies.forEach((entry) => {
      const name = entry.body.name ?? "body";
      const priority = PRIORITY_BODIES.has(name) ? 10 : 1;
      const [x, y, z] = applyWorldShift(entry.position, focus, displayScale);
      const radius = Math.max((entry.body.radiusMeters ?? 1000) * displayScale, 0.15);
      items.push({
        key: `body-${name}`,
        text: name,
        position: [x, y + radius + 0.4, z],
        priority,
      });
    });
    model.routeAnchors.forEach((anchor, index) => {
      const role = anchor.role ?? "anchor";
      if (role !== "encounter" && role !== "escape") {
        return;
      }
      const [x, y, z] = applyWorldShift(anchor.position, focus, displayScale);
      items.push({
        key: `route-${index}`,
        text: `${anchor.targetBody ?? ""} ${role}`,
        position: [x, y + 0.6, z],
        priority: 8,
      });
    });
    items.sort((a, b) => b.priority - a.priority);
    return items.slice(0, 24);
  }, [model, focus, displayScale, quality.labelsEnabled]);

  if (!labels.length) {
    return null;
  }

  return (
    <group>
      {labels.map((label) => (
        <Html
          key={label.key}
          position={label.position}
          center
          distanceFactor={12}
          style={{
            pointerEvents: "none",
            color: "#e7f0f7",
            fontSize: "11px",
            fontFamily: "Segoe UI, sans-serif",
            textShadow: "0 1px 3px #000",
            whiteSpace: "nowrap",
            userSelect: "none",
          }}
        >
          {label.text}
        </Html>
      ))}
    </group>
  );
}
