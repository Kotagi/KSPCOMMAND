import { Bloom, EffectComposer } from "@react-three/postprocessing";
import { Stars } from "@react-three/drei";
import { useViewStore } from "../../store/viewStore";

export function SceneEffects() {
  const quality = useViewStore((s) => s.getQuality());

  return (
    <>
      <Stars
        radius={300}
        depth={50}
        count={quality.starCount}
        factor={4}
        saturation={0}
        fade
        speed={0.5}
      />
      {quality.bloomEnabled && (
        <EffectComposer>
          <Bloom luminanceThreshold={0.6} luminanceSmoothing={0.9} intensity={1.2} />
        </EffectComposer>
      )}
    </>
  );
}
