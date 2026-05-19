import { create } from "zustand";
import type { TelemetrySnapshot } from "../telemetry/schema-v6";
import {
  buildSolarSystemModel,
  type SolarSystemModel,
} from "../model/buildSolarSystemModel";

export type CameraMode =
  | "fullSystem"
  | "activeVessel"
  | "currentReferenceBody"
  | "encounterBody"
  | "route";

export type SolarRenderMode = "3d" | "2d";

interface ViewState {
  telemetry: TelemetrySnapshot | null;
  model: SolarSystemModel | null;
  cameraMode: CameraMode;
  displayScale: number;
  focusBodyName: string | null;
  scrubEnabled: boolean;
  scrubUniversalTime: number | null;
  solarRenderMode: SolarRenderMode;
  selectedObjectId: string | null;
  userInteractedCamera: boolean;
  setTelemetry: (telemetry: TelemetrySnapshot | null) => void;
  setCameraMode: (mode: CameraMode) => void;
  setDisplayScale: (scale: number) => void;
  setFocusBodyName: (name: string | null) => void;
  setScrubEnabled: (enabled: boolean) => void;
  setScrubUniversalTime: (ut: number | null) => void;
  setSolarRenderMode: (mode: SolarRenderMode) => void;
  setSelectedObjectId: (id: string | null) => void;
  setUserInteractedCamera: (value: boolean) => void;
  rebuildModel: () => void;
}

function rebuild(
  telemetry: TelemetrySnapshot | null,
  scrubEnabled: boolean,
  scrubUniversalTime: number | null,
): SolarSystemModel | null {
  if (!telemetry) {
    return null;
  }
  return buildSolarSystemModel(telemetry, { scrubEnabled, scrubUniversalTime });
}

export const useViewStore = create<ViewState>((set, get) => ({
  telemetry: null,
  model: null,
  cameraMode: "fullSystem",
  displayScale: 1e-9,
  focusBodyName: null,
  scrubEnabled: false,
  scrubUniversalTime: null,
  solarRenderMode: "3d",
  selectedObjectId: null,
  userInteractedCamera: false,
  setTelemetry: (telemetry) => {
    const { scrubEnabled, scrubUniversalTime } = get();
    set({
      telemetry,
      model: rebuild(telemetry, scrubEnabled, scrubUniversalTime),
    });
  },
  setCameraMode: (cameraMode) => set({ cameraMode }),
  setDisplayScale: (displayScale) => set({ displayScale }),
  setFocusBodyName: (focusBodyName) => set({ focusBodyName }),
  setScrubEnabled: (scrubEnabled) => {
    set({ scrubEnabled });
    get().rebuildModel();
  },
  setScrubUniversalTime: (scrubUniversalTime) => {
    set({ scrubUniversalTime });
    get().rebuildModel();
  },
  setSolarRenderMode: (solarRenderMode) => set({ solarRenderMode }),
  setSelectedObjectId: (selectedObjectId) => set({ selectedObjectId }),
  setUserInteractedCamera: (userInteractedCamera) => set({ userInteractedCamera }),
  rebuildModel: () => {
    const { telemetry, scrubEnabled, scrubUniversalTime } = get();
    set({ model: rebuild(telemetry, scrubEnabled, scrubUniversalTime) });
  },
}));
