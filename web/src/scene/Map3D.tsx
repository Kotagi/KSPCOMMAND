import { Canvas } from "@react-three/fiber";
import { BodiesLayer } from "./layers/BodiesLayer";
import { SoiLayer } from "./layers/SoiLayer";
import { RouteLayer } from "./layers/RouteLayer";
import { VesselLayer } from "./layers/VesselLayer";
import { OrbitsLayer, PlacementMarkersLayer } from "./layers/OrbitsLayer";
import { BodyOrbitsLayer } from "./layers/BodyOrbitsLayer";
import { SceneEffects } from "./layers/Effects";
import { AtmosphereLayer } from "./layers/AtmosphereLayer";
import { ApsisMarkersLayer } from "./layers/ApsisMarkersLayer";
import { LabelsLayer } from "./layers/LabelsLayer";
import { CameraRig } from "./CameraRig";
import { SelectionController } from "./SelectionController";

export function Map3D() {
  return (
    <Canvas
      gl={{ logarithmicDepthBuffer: true, antialias: true }}
      camera={{ position: [25, 18, 25], fov: 50, near: 0.01, far: 100000 }}
      style={{ width: "100%", height: "100%", background: "#071019" }}
    >
      <color attach="background" args={["#071019"]} />
      <ambientLight intensity={0.35} />
      <directionalLight position={[10, 20, 10]} intensity={1.1} />
      <SceneEffects />
      <BodiesLayer />
      <AtmosphereLayer />
      <SoiLayer />
      <BodyOrbitsLayer />
      <OrbitsLayer />
      <RouteLayer />
      <PlacementMarkersLayer />
      <ApsisMarkersLayer />
      <VesselLayer />
      <LabelsLayer />
      <SelectionController />
      <CameraRig />
    </Canvas>
  );
}
