import { useMemo } from "react";
import { Line } from "@react-three/drei";
import type { OrbitPatch, Vector3 } from "../../telemetry/schema-v6";
import { conicToInertialPath } from "../../math/buildConicGeometry";
import { finiteOr } from "../../math/util";
import { useViewStore } from "../../store/viewStore";
import { applyWorldShift, getFocusPosition } from "../../coords/worldShift";
function findPatchStartPosition(patch: OrbitPatch): Vector3 | null {
  const samples = patch.placementSamples ?? [];
  for (const sample of samples) {
    if (sample.sampleRole === "patchStart" && sample.positionRootRelativeMeters) {
      return sample.positionRootRelativeMeters;
    }
  }
  return patch.referenceBodyPositionRootRelativeMeters ?? null;
}

function translateInertialToRoot(
  inertialPoints: Vector3[],
  anchor: Vector3 | null,
): Vector3[] {
  if (!anchor) {
    return inertialPoints;
  }
  return inertialPoints.map((p) => ({
    x: anchor.x + p.x,
    y: anchor.y + p.y,
    z: anchor.z + p.z,
  }));
}

export function OrbitsLayer() {
  const telemetry = useViewStore((s) => s.telemetry);
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);

  const focus = useMemo(() => {
    if (!model || !focusBodyName) {
      return null;
    }
    return getFocusPosition(model.bodies, focusBodyName);
  }, [model, focusBodyName]);

  const patchLines = useMemo(() => {
    const patches = telemetry?.orbitPatches ?? [];
    const bodyByName = new Map(
      (telemetry?.bodies ?? []).map((b) => [b.name, b]),
    );
    return patches
      .map((patch, index) => {
        if (!patch) {
          return null;
        }
        const refName = patch.referenceBody;
        const refBody = refName ? bodyByName.get(refName) : undefined;
        const bodyRadius = finiteOr(
          patch.referenceBodyRadiusMeters ?? refBody?.radiusMeters,
          1000,
        );
        const inertial = conicToInertialPath(patch, bodyRadius, 200);
        if (inertial.length < 2) {
          return null;
        }
        const mode = patch.patchPlacementMode ?? "";
        const useSampled =
          mode === "multiSampleEphemeris" || mode === "multiSampleEphemerisPartial";
        const anchor = useSampled ? findPatchStartPosition(patch) : null;
        const rootPath = translateInertialToRoot(inertial, anchor);
        const threePoints = rootPath.map((p) => {
          const shifted = applyWorldShift(p, focus, displayScale);
          return shifted as [number, number, number];
        });
        return {
          key: `patch-${index}`,
          points: threePoints,
          active: !!patch.isActivePatch,
        };
      })
      .filter(Boolean) as { key: string; points: [number, number, number][]; active: boolean }[];
  }, [telemetry, focus, displayScale]);

  return (
    <group>
      {patchLines.map((line) => (
        <Line
          key={line.key}
          points={line.points}
          color={line.active ? "#67d3ff" : "#4a6a80"}
          lineWidth={line.active ? 2 : 1}
          transparent
          opacity={line.active ? 0.95 : 0.45}
        />
      ))}
    </group>
  );
}

/** Scrub highlight markers at sampled placements */
export function PlacementMarkersLayer() {
  const model = useViewStore((s) => s.model);
  const displayScale = useViewStore((s) => s.displayScale);
  const focusBodyName = useViewStore((s) => s.focusBodyName);
  const scrubEnabled = useViewStore((s) => s.scrubEnabled);

  const focus = useMemo(() => {
    if (!model || !focusBodyName) {
      return null;
    }
    return getFocusPosition(model.bodies, focusBodyName);
  }, [model, focusBodyName]);

  if (!model?.canDraw) {
    return null;
  }

  return (
    <group>
      {model.placementMarkers.map((marker, i) => {
        const [x, y, z] = applyWorldShift(marker.position, focus, displayScale);
        const isEncounter = marker.role === "encounter";
        const dim = isEncounter ? 0.35 : 0.2;
        return (
          <mesh key={`marker-${i}`} position={[x, y, z]}>
            <boxGeometry args={[dim, dim, dim]} />
            <meshStandardMaterial
              color={isEncounter ? "#ff6b6b" : "#ffd166"}
              emissive={isEncounter ? "#ff6b6b" : "#ffd166"}
              emissiveIntensity={scrubEnabled ? 0.8 : 0.3}
            />
          </mesh>
        );
      })}
    </group>
  );
}
