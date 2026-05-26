import { useEffect, useMemo, useState } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import * as THREE from "three";
import { resolveBodyTextureUrl } from "../assets/planetBodyTextures";
import {
  kspRootVectorToThree,
  rotateKspRootVector,
} from "../coords/kspBodyOrientation";
import {
  fetchLabOrientationFromTelemetry,
  labOrientationExampleUrls,
  parseLabOrientationFromSearch,
  type LabOrientationConfig,
} from "./labOrientationConfig";
import { PlanetBodyMeshPoleFrame } from "../scene/v3/layers/PlanetBodyMeshPoleFrame";
import { PlanetBodyOrientedGroup } from "../scene/v3/layers/PlanetBodyOrientedGroup";
import { TexturedPlanetBody } from "../scene/v3/layers/TexturedPlanetBody";

const DEFAULT_TEXTURE = "/assets/bodies/Kerbin.jpg";

function labTextureParams(): {
  textureUrl: string;
  revision?: string;
  variant: string;
} {
  const params = new URLSearchParams(window.location.search);
  return {
    textureUrl: params.get("textureUrl") ?? DEFAULT_TEXTURE,
    revision: params.get("rev") ?? undefined,
    variant: params.get("variant") ?? "orientation-poc",
  };
}

function LabOrientedSphere({
  textureUrl,
  revision,
  orientation,
}: {
  textureUrl: string;
  revision?: string;
  orientation: LabOrientationConfig;
}) {
  const northHint = useMemo(() => {
    const northKsp = rotateKspRootVector(orientation.orientationKsp, {
      x: 0,
      y: 1,
      z: 0,
    });
    const v = kspRootVectorToThree(northKsp).normalize().multiplyScalar(1.2);
    return [v.x, v.y, v.z] as [number, number, number];
  }, [orientation.orientationKsp]);

  return (
    <PlanetBodyOrientedGroup
      orientationKsp={orientation.orientationKsp}
      angularVelocityKsp={orientation.angularVelocityKsp}
      frameSpin={orientation.extrapolateSpin}
    >
      <PlanetBodyMeshPoleFrame>
        <TexturedPlanetBody
          radius={1}
          position={[0, 0, 0]}
          renderOrder={1}
          fallbackColor="#4ecdc4"
          textureUrl={textureUrl}
          textureRevision={revision}
        />
      </PlanetBodyMeshPoleFrame>
      <mesh position={northHint}>
        <sphereGeometry args={[0.05, 10, 10]} />
        <meshBasicMaterial color="#ffe066" />
      </mesh>
    </PlanetBodyOrientedGroup>
  );
}

/** Isolated sphere — texture + orientation POC (Phase 3.4). See web/dev/README.md */
export function PlanetTextureLab() {
  const { textureUrl, revision, variant } = labTextureParams();
  const resolved = resolveBodyTextureUrl(textureUrl);
  const params = new URLSearchParams(window.location.search);
  const telemetryBody = params.get("body") ?? "Kerbin";
  const wantsTelemetry =
    params.get("orientation") === "telemetry" ||
    params.get("orient") === "telemetry";

  const [orientation, setOrientation] = useState<LabOrientationConfig>(() =>
    parseLabOrientationFromSearch(window.location.search),
  );
  const [telemetryError, setTelemetryError] = useState<string | null>(null);

  useEffect(() => {
    if (!wantsTelemetry) {
      setOrientation(parseLabOrientationFromSearch(window.location.search));
      setTelemetryError(null);
      return;
    }

    let cancelled = false;
    fetchLabOrientationFromTelemetry(telemetryBody).then((result) => {
      if (cancelled) {
        return;
      }
      if ("error" in result) {
        setTelemetryError(result.error);
        setOrientation(parseLabOrientationFromSearch("?orientation=tilt-and-spin&spin=1"));
        return;
      }
      setTelemetryError(null);
      setOrientation(result);
    });

    return () => {
      cancelled = true;
    };
  }, [wantsTelemetry, telemetryBody]);

  useEffect(() => {
    fetch(resolved, { method: "HEAD" })
      .then((r) => {
        console.log("[KspWebMap] lab texture HEAD", r.status, r.headers.get("content-type"));
      })
      .catch((e) => console.warn("[KspWebMap] lab texture HEAD failed", e));
  }, [resolved]);

  const examples = labOrientationExampleUrls(window.location.href.split("?")[0]);
  const q = orientation.orientationKsp;

  return (
    <div
      style={{
        width: "100vw",
        height: "100vh",
        background: "#071019",
        display: "flex",
        flexDirection: "column",
      }}
    >
      <div
        style={{
          padding: "8px 12px",
          color: "#9db1c3",
          fontFamily: "system-ui, sans-serif",
          fontSize: 13,
          lineHeight: 1.5,
          maxHeight: "42vh",
          overflowY: "auto",
        }}
      >
        <div>
          Planet orientation lab — {variant}
        </div>
        <div>Texture: {textureUrl}</div>
        <div>Preset: {orientation.preset} — {orientation.sourceLabel}</div>
        <div>
          q=({q.x.toFixed(3)}, {q.y.toFixed(3)}, {q.z.toFixed(3)}, {q.w.toFixed(3)})
          {orientation.extrapolateSpin ? " · spin ON" : " · spin off"}
        </div>
        {telemetryError ? (
          <div style={{ color: "#ffb347", marginTop: 4 }}>Telemetry: {telemetryError}</div>
        ) : null}
        <div style={{ color: "#61d394", marginTop: 4 }}>
          Yellow dot = north pole in root frame. Axes = Three world (after basis change).
          Drag to orbit camera — tilt presets should look oblique vs identity.
        </div>
        <div style={{ marginTop: 6, fontSize: 12 }}>
          <div>Try:</div>
          {examples.map((url) => (
            <div key={url}>
              <a href={url} style={{ color: "#7eb8ff" }}>
                {url.replace(/^.*\//, "")}
              </a>
            </div>
          ))}
        </div>
      </div>
      <Canvas
        style={{ flex: 1 }}
        camera={{ position: [0, 0, 3.2], fov: 45, near: 0.01, far: 100 }}
        gl={{
          antialias: true,
          toneMapping: THREE.NoToneMapping,
          preserveDrawingBuffer: true,
        }}
        onCreated={({ gl }) => {
          gl.toneMapping = THREE.NoToneMapping;
          gl.outputColorSpace = THREE.SRGBColorSpace;
        }}
      >
        <color attach="background" args={["#071019"]} />
        <ambientLight intensity={0.35} />
        <axesHelper args={[1.35]} />
        <LabOrientedSphere
          textureUrl={textureUrl}
          revision={revision}
          orientation={orientation}
        />
        <OrbitControls enablePan={false} minDistance={1.5} maxDistance={8} />
      </Canvas>
    </div>
  );
}
