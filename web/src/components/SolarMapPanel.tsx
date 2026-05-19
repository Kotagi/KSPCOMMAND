import { Map3D } from "../scene/Map3D";
import { MapHud } from "./MapHud";
import { useViewStore } from "../store/viewStore";
import "./solar-map.css";

export function SolarMapPanel() {
  const solarRenderMode = useViewStore((s) => s.solarRenderMode);

  return (
    <div className="ksp-solar-panel">
      <MapHud />
      <div className="ksp-solar-viewport">
        {solarRenderMode === "3d" ? (
          <Map3D />
        ) : (
          <div className="ksp-solar-2d-placeholder">
            Use legacy 2D canvas in dashboard (toggle View → 2D shows this host; canvas renders
            when embedded in index.html).
          </div>
        )}
      </div>
    </div>
  );
}
