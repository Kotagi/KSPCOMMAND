import type { CameraMode } from "../store/viewStore";
import { useViewStore } from "../store/viewStore";

const CAMERA_MODES: { id: CameraMode; label: string }[] = [
  { id: "fullSystem", label: "Full system" },
  { id: "activeVessel", label: "Vessel" },
  { id: "currentReferenceBody", label: "Reference" },
  { id: "encounterBody", label: "Encounter" },
  { id: "route", label: "Route" },
];

export function MapHud() {
  const model = useViewStore((s) => s.model);
  const cameraMode = useViewStore((s) => s.cameraMode);
  const displayScale = useViewStore((s) => s.displayScale);
  const scrubEnabled = useViewStore((s) => s.scrubEnabled);
  const scrubUniversalTime = useViewStore((s) => s.scrubUniversalTime);
  const solarRenderMode = useViewStore((s) => s.solarRenderMode);
  const setCameraMode = useViewStore((s) => s.setCameraMode);
  const setDisplayScale = useViewStore((s) => s.setDisplayScale);
  const setScrubEnabled = useViewStore((s) => s.setScrubEnabled);
  const setScrubUniversalTime = useViewStore((s) => s.setScrubUniversalTime);
  const setSolarRenderMode = useViewStore((s) => s.setSolarRenderMode);
  const setUserInteractedCamera = useViewStore((s) => s.setUserInteractedCamera);

  const utMin = model?.telemetry?.gameUniversalTimeSeconds ?? 0;
  const utMax = utMin + 86400 * 30;

  return (
    <div className="ksp-solar-hud">
      <div className="ksp-solar-hud-row">
        <label>
          View{" "}
          <select
            value={solarRenderMode}
            onChange={(e) => setSolarRenderMode(e.target.value as "3d" | "2d")}
          >
            <option value="3d">3D WebGL</option>
            <option value="2d">2D Canvas (legacy)</option>
          </select>
        </label>
        <label>
          Display scale{" "}
          <input
            type="range"
            min={6}
            max={12}
            step={0.1}
            value={Math.log10(displayScale)}
            onChange={(e) => setDisplayScale(10 ** Number(e.target.value))}
          />
          <span>{displayScale.toExponential(1)} m/unit</span>
        </label>
      </div>
      <div className="ksp-solar-hud-row">
        {CAMERA_MODES.map((mode) => (
          <button
            key={mode.id}
            type="button"
            className={cameraMode === mode.id ? "active" : ""}
            onClick={() => {
              setUserInteractedCamera(false);
              setCameraMode(mode.id);
            }}
          >
            {mode.label}
          </button>
        ))}
      </div>
      <div className="ksp-solar-hud-row">
        <label>
          <input
            type="checkbox"
            checked={scrubEnabled}
            onChange={(e) => setScrubEnabled(e.target.checked)}
          />
          Ephemeris scrub (read-only)
        </label>
        {scrubEnabled && (
          <input
            type="range"
            min={utMin}
            max={utMax}
            value={scrubUniversalTime ?? utMin}
            onChange={(e) => setScrubUniversalTime(Number(e.target.value))}
          />
        )}
      </div>
      <div className="ksp-solar-truth-banner">
        <strong>Truth:</strong> KSP patched-conic prediction — not SPICE/N-body.{" "}
        {model?.routeOverlayMode && model.routeOverlayMode !== "none"
          ? `Route: ${model.routeOverlayMode}. `
          : ""}
        Ephemeris: {model?.ephemerisStatus ?? "N/A"}. Placement:{" "}
        {model?.placementMode ?? "N/A"}. Patch chain: {model?.patchChainStatus ?? "N/A"}.
        {model?.ephemerisValidationResidualMeters != null && (
          <> Validation residual: {model.ephemerisValidationResidualMeters.toFixed(1)} m.</>
        )}
      </div>
      {!model?.canDraw && model?.reason && (
        <div className="ksp-solar-warning">{model.reason}</div>
      )}
    </div>
  );
}
