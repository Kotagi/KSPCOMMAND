import { createRoot, type Root } from "react-dom/client";
import { SolarMapPanel } from "./components/SolarMapPanel";
import { useViewStore, type CameraMode, type SolarRenderMode } from "./store/viewStore";
import type { TelemetrySnapshot } from "./telemetry/schema-v6";
import type { SolarSystemModel } from "./model/buildSolarSystemModel";
import type { SelectionDetail } from "./selection/types";
import { syncDashboardSolarView } from "./shell/syncDashboardView";

export interface KspSolarMapApi {
  mount: (container: HTMLElement) => void;
  unmount: () => void;
  updateTelemetry: (telemetry: TelemetrySnapshot | null) => void;
  getSolarRenderMode: () => SolarRenderMode;
  getCameraMode: () => CameraMode;
  setCameraMode: (mode: CameraMode) => void;
  setScrubEnabled: (enabled: boolean) => void;
  setScrubUniversalTime: (ut: number | null) => void;
  recenter: () => void;
  resetView: () => void;
  onSelectionChange: (callback: (detail: SelectionDetail | null) => void) => () => void;
  getModel: () => SolarSystemModel | null;
  getSelectionDetail: () => SelectionDetail | null;
  getHoverObjectId: () => string | null;
  getSolarFullscreen: () => boolean;
  setSolarFullscreen: (enabled: boolean) => void;
  toggleSolarFullscreen: () => void;
  syncDashboardView: () => void;
}

let root: Root | null = null;
let containerEl: HTMLElement | null = null;
const selectionListeners = new Set<(detail: SelectionDetail | null) => void>();

function notifySelection(detail: SelectionDetail | null) {
  selectionListeners.forEach((cb) => cb(detail));
}

useViewStore.subscribe((state, prev) => {
  if (state.selectionDetail !== prev.selectionDetail) {
    notifySelection(state.selectionDetail);
  }
  if (state.solarRenderMode !== prev.solarRenderMode) {
    syncDashboardSolarView(state.solarRenderMode);
  }
});

function MountApp() {
  return <SolarMapPanel />;
}

function ensureMounted() {
  if (root) {
    return;
  }
  const container = document.getElementById("solar3dRoot");
  if (container) {
    api.mount(container);
    container.dataset.mounted = "1";
  }
}

const api: KspSolarMapApi = {
  mount(container: HTMLElement) {
    if (root) {
      api.unmount();
    }
    containerEl = container;
    root = createRoot(container);
    root.render(<MountApp />);
    syncDashboardSolarView(useViewStore.getState().solarRenderMode);
  },
  unmount() {
    if (root) {
      root.unmount();
      root = null;
    }
    if (containerEl) {
      containerEl.innerHTML = "";
      containerEl = null;
    }
    selectionListeners.clear();
  },
  updateTelemetry(telemetry: TelemetrySnapshot | null) {
    ensureMounted();
    useViewStore.getState().setTelemetry(telemetry);
  },
  getSolarRenderMode() {
    return useViewStore.getState().solarRenderMode;
  },
  getCameraMode() {
    return useViewStore.getState().cameraMode;
  },
  setCameraMode(mode: CameraMode) {
    ensureMounted();
    useViewStore.getState().setCameraMode(mode);
  },
  setScrubEnabled(enabled: boolean) {
    ensureMounted();
    useViewStore.getState().setScrubEnabled(enabled);
  },
  setScrubUniversalTime(ut: number | null) {
    ensureMounted();
    useViewStore.getState().setScrubUniversalTime(ut);
  },
  recenter() {
    ensureMounted();
    useViewStore.getState().recenter();
  },
  resetView() {
    ensureMounted();
    useViewStore.getState().resetView();
  },
  onSelectionChange(callback) {
    selectionListeners.add(callback);
    callback(useViewStore.getState().selectionDetail);
    return () => {
      selectionListeners.delete(callback);
    };
  },
  getModel() {
    return useViewStore.getState().model;
  },
  getSelectionDetail() {
    return useViewStore.getState().selectionDetail;
  },
  getHoverObjectId() {
    return useViewStore.getState().hoverObjectId;
  },
  getSolarFullscreen() {
    return useViewStore.getState().solarFullscreen;
  },
  setSolarFullscreen(enabled: boolean) {
    ensureMounted();
    useViewStore.getState().setSolarFullscreen(enabled);
  },
  toggleSolarFullscreen() {
    ensureMounted();
    useViewStore.getState().toggleSolarFullscreen();
  },
  syncDashboardView() {
    syncDashboardSolarView(useViewStore.getState().solarRenderMode);
  },
};

declare global {
  interface Window {
    KspSolarMap?: KspSolarMapApi;
    KspSolarMapUiVersion?: string;
  }
}

/** Bumped when web UI changes; check in devtools if map looks stale. */
export const KSP_WEB_MAP_UI_VERSION = "76-closed-orbit-ring-fix";

window.KspSolarMap = api;
window.KspSolarMapUiVersion = KSP_WEB_MAP_UI_VERSION;
console.log("[KspWebMap] UI", KSP_WEB_MAP_UI_VERSION);
window.dispatchEvent(new CustomEvent("ksp-solar-map-ready"));
export default api;
