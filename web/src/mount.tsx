import { createRoot, type Root } from "react-dom/client";
import { SolarMapPanel } from "./components/SolarMapPanel";
import { useViewStore } from "./store/viewStore";
import type { TelemetrySnapshot } from "./telemetry/schema-v6";

export interface KspSolarMapApi {
  mount: (container: HTMLElement) => void;
  unmount: () => void;
  updateTelemetry: (telemetry: TelemetrySnapshot | null) => void;
  getSolarRenderMode: () => "3d" | "2d";
}

let root: Root | null = null;
let containerEl: HTMLElement | null = null;

function MountApp() {
  return <SolarMapPanel />;
}

const api: KspSolarMapApi = {
  mount(container: HTMLElement) {
    if (root) {
      api.unmount();
    }
    containerEl = container;
    root = createRoot(container);
    root.render(<MountApp />);
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
  },
  updateTelemetry(telemetry: TelemetrySnapshot | null) {
    useViewStore.getState().setTelemetry(telemetry);
  },
  getSolarRenderMode() {
    return useViewStore.getState().solarRenderMode;
  },
};

declare global {
  interface Window {
    KspSolarMap?: KspSolarMapApi;
  }
}

window.KspSolarMap = api;
export default api;
