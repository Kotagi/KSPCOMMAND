import { Bloom, EffectComposer } from "@react-three/postprocessing";
import { Stars } from "@react-three/drei";

export function SceneEffects() {
  return (
    <>
      <Stars radius={300} depth={50} count={3000} factor={4} saturation={0} fade speed={0.5} />
      <EffectComposer>
        <Bloom luminanceThreshold={0.6} luminanceSmoothing={0.9} intensity={1.2} />
      </EffectComposer>
    </>
  );
}
