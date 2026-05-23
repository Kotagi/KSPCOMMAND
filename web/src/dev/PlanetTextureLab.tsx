import { useEffect } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import * as THREE from "three";
import { resolveBodyTextureUrl } from "../assets/planetBodyTextures";
import { TexturedPlanetBody } from "../scene/v3/layers/TexturedPlanetBody";

const DEFAULT_TEXTURE = "/assets/bodies/Kerbin.jpg";

function labParams(): { textureUrl: string; revision?: string; variant: string } {
  const params = new URLSearchParams(window.location.search);
  return {
    textureUrl: params.get("textureUrl") ?? DEFAULT_TEXTURE,
    revision: params.get("rev") ?? undefined,
    variant: params.get("variant") ?? "material-ref",
  };
}

function LabSphere({
  textureUrl,
  revision,
}: {
  textureUrl: string;
  revision?: string;
}) {
  return (
    <TexturedPlanetBody
      radius={1}
      position={[0, 0, 0]}
      renderOrder={1}
      fallbackColor="#4ecdc4"
      textureUrl={textureUrl}
      textureRevision={revision}
    />
  );
}

/** Isolated sphere for verifying body texture application (see web/dev/README.md). */
export function PlanetTextureLab() {
  const { textureUrl, revision, variant } = labParams();
  const resolved = resolveBodyTextureUrl(textureUrl);

  useEffect(() => {
    fetch(resolved, { method: "HEAD" })
      .then((r) => {
        console.log("[KspWebMap] lab texture HEAD", r.status, r.headers.get("content-type"));
      })
      .catch((e) => console.warn("[KspWebMap] lab texture HEAD failed", e));
  }, [resolved]);

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
        }}
      >
        <div>
          Planet texture lab — {variant}
        </div>
        <div>Request: {textureUrl}</div>
        <div>Resolved: {resolved}</div>
        {revision ? <div>rev={revision}</div> : null}
        <div style={{ color: "#61d394", marginTop: 4 }}>
          Expect colored Kerbin (blue/green). White = map not bound; teal = still loading.
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
        <LabSphere textureUrl={textureUrl} revision={revision} />
        <OrbitControls enablePan={false} minDistance={1.5} maxDistance={8} />
      </Canvas>
    </div>
  );
}
