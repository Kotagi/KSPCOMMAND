import { useMemo } from "react";
import { Line } from "@react-three/drei";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";

export function RouteLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);

  const focus = useMemo(() => {
    if (!model || !focusBodyName) {
      return null;
    }
    return getFocusPosition(model.bodies, focusBodyName);
  }, [model, focusBodyName]);

  const points = useMemo(() => {
    if (!model || model.routeAnchors.length < 2) {
      return [];
    }
    return model.routeAnchors.map((anchor) =>
      applyWorldShift(anchor.position, focus, displayScale),
    );
  }, [model, focus, displayScale]);

  if (points.length < 2) {
    return null;
  }

  const isPrediction = model?.routeOverlayMode.includes("patched-conic");
  return (
    <Line
      points={points}
      color={isPrediction ? "#ffd166" : "#ff6b6b"}
      lineWidth={2}
      dashed={!isPrediction}
      dashSize={0.5}
      gapSize={0.25}
    />
  );
}
