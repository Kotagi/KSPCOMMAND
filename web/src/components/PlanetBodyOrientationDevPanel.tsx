import { useViewStore } from "../store/viewStore";

/** Dev HUD: show spin/tilt axis through planet mesh LOD bodies. */
export function PlanetBodyOrientationDevPanel() {
  const visible = useViewStore((s) => s.devPlanetBodySpinAxisVisible);
  const setVisible = useViewStore((s) => s.setDevPlanetBodySpinAxisVisible);
  const renderMode = useViewStore((s) => s.solarRenderMode);
  const lodOverride = useViewStore((s) => s.devPlanetBodyLodOverride);
  const v3 = renderMode === "3d-v3";

  return (
    <div className="ksp-solar-customize-map-dev">
      <span className="ksp-solar-customize-map-dev-toggle">Dev: planet orientation</span>
      <label className="ksp-solar-customize-map-dev-toggle">
        <input
          type="checkbox"
          checked={visible}
          disabled={!v3}
          onChange={(e) => setVisible(e.target.checked)}
        />
        Show spin/tilt axis
      </label>
      {!v3 ? (
        <span className="ksp-solar-customize-map-dev-hint">
          Switch render mode to <strong>3d-v3</strong> to use orientation overlays.
        </span>
      ) : lodOverride === "icon" ? (
        <span className="ksp-solar-customize-map-dev-hint">
          Axis hidden while <strong>Force dot</strong> is on (mesh LOD only).
        </span>
      ) : visible ? (
        <span className="ksp-solar-customize-map-dev-hint">
          Yellow line = spin axis through mesh poles (KSP north); body should rotate about this line.
        </span>
      ) : null}
    </div>
  );
}
