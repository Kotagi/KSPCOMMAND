import { useEffect, useMemo, useRef } from "react";
import { useThree } from "@react-three/fiber";
import * as THREE from "three";
import { useViewStore } from "../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../coords/worldShift";
import {
  buildSelectionTargets,
  findSelectionDetail,
} from "../selection/buildSelectionTargets";

export function SelectionController() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const cameraMode = useViewStore((s) => s.cameraMode);
  const setSelectionDetail = useViewStore((s) => s.setSelectionDetail);
  const setHoverObjectId = useViewStore((s) => s.setHoverObjectId);
  const { camera, gl } = useThree();
  const raycaster = useRef(new THREE.Raycaster());
  const pointer = useRef(new THREE.Vector2());

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

  const spheres = useMemo(() => {
    if (!model?.canDraw) {
      return [];
    }
    return buildSelectionTargets(model).map((target) => {
      const [x, y, z] = applyWorldShift(
        { x: target.position[0], y: target.position[1], z: target.position[2] },
        focus,
        displayScale,
      );
      const radius = Math.max(target.radius * displayScale, 0.2);
      return { ...target, scenePos: new THREE.Vector3(x, y, z), sceneRadius: radius };
    });
  }, [model, focus, displayScale]);

  useEffect(() => {
    const dom = gl.domElement;

    function pick(clientX: number, clientY: number): string | null {
      if (!spheres.length) {
        return null;
      }
      const rect = dom.getBoundingClientRect();
      pointer.current.x = ((clientX - rect.left) / rect.width) * 2 - 1;
      pointer.current.y = -((clientY - rect.top) / rect.height) * 2 + 1;
      raycaster.current.setFromCamera(pointer.current, camera);
      let bestId: string | null = null;
      let bestDist = Infinity;
      spheres.forEach((sphere) => {
        const distSq = raycaster.current.ray.distanceSqToPoint(sphere.scenePos);
        const threshold = sphere.sceneRadius * 1.5;
        if (distSq <= threshold * threshold && distSq < bestDist) {
          bestDist = distSq;
          bestId = sphere.id;
        }
      });
      return bestId;
    }

    function onClick(event: MouseEvent) {
      if (!model?.canDraw) {
        return;
      }
      const id = pick(event.clientX, event.clientY);
      const detail = findSelectionDetail(buildSelectionTargets(model), id);
      setSelectionDetail(detail);
    }

    function onMove(event: MouseEvent) {
      const id = pick(event.clientX, event.clientY);
      setHoverObjectId(id);
    }

    dom.addEventListener("click", onClick);
    dom.addEventListener("mousemove", onMove);
    return () => {
      dom.removeEventListener("click", onClick);
      dom.removeEventListener("mousemove", onMove);
    };
  }, [gl, camera, spheres, model, setSelectionDetail, setHoverObjectId]);

  return null;
}
