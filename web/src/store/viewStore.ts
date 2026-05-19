import { create } from "zustand";
import type { TelemetrySnapshot, Vector3 } from "../telemetry/schema-v6";
import {
  buildSolarSystemModel,
  type SolarSystemModel,
} from "../model/buildSolarSystemModel";
import type { QualityPreset } from "../settings/qualityStore";
import { getQualitySettings } from "../settings/qualityStore";
import type { SelectionDetail } from "../selection/types";

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
  selectionDetail: SelectionDetail | null;
  hoverObjectId: string | null;
  userInteractedCamera: boolean;
  qualityPreset: QualityPreset;
  cameraFitNonce: number;
  vesselDisplayPosition: Vector3 | null;
  vesselTargetPosition: Vector3 | null;
  setTelemetry: (telemetry: TelemetrySnapshot | null) => void;
  setCameraMode: (mode: CameraMode) => void;
  setDisplayScale: (scale: number) => void;
  setFocusBodyName: (name: string | null) => void;
  setScrubEnabled: (enabled: boolean) => void;
  setScrubUniversalTime: (ut: number | null) => void;
  setSolarRenderMode: (mode: SolarRenderMode) => void;
  setSelectedObjectId: (id: string | null) => void;
  setSelectionDetail: (detail: SelectionDetail | null) => void;
  setHoverObjectId: (id: string | null) => void;
  setUserInteractedCamera: (value: boolean) => void;
  setQualityPreset: (preset: QualityPreset) => void;
  requestCameraFit: () => void;
  recenter: () => void;
  resetView: () => void;
  rebuildModel: () => void;
  getQuality: () => ReturnType<typeof getQualitySettings>;
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

function focusForMode(model: SolarSystemModel | null, mode: CameraMode): string | null {
  if (!model) {
    return null;
  }
  if (mode === "currentReferenceBody" && model.referenceBody) {
    return model.referenceBody;
  }
  if (mode === "encounterBody" && model.encounterBody) {
    return model.encounterBody;
  }
  if (mode === "activeVessel" && model.referenceBody) {
    return model.referenceBody;
  }
  return null;
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
  selectionDetail: null,
  hoverObjectId: null,
  userInteractedCamera: false,
  qualityPreset: "medium",
  cameraFitNonce: 0,
  vesselDisplayPosition: null,
  vesselTargetPosition: null,
  setTelemetry: (telemetry) => {
    const { scrubEnabled, scrubUniversalTime, vesselDisplayPosition } = get();
    const model = rebuild(telemetry, scrubEnabled, scrubUniversalTime);
    const nextTarget = model?.vesselPosition ?? null;
    set({
      telemetry,
      model,
      vesselTargetPosition: nextTarget,
      vesselDisplayPosition: vesselDisplayPosition ?? nextTarget,
      focusBodyName: focusForMode(model, get().cameraMode),
    });
  },
  setCameraMode: (cameraMode) => {
    const { model } = get();
    set({
      cameraMode,
      focusBodyName: focusForMode(model, cameraMode),
      userInteractedCamera: false,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
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
  setSelectionDetail: (selectionDetail) =>
    set({
      selectionDetail,
      selectedObjectId: selectionDetail?.id ?? null,
    }),
  setHoverObjectId: (hoverObjectId) => set({ hoverObjectId }),
  setUserInteractedCamera: (userInteractedCamera) => set({ userInteractedCamera }),
  setQualityPreset: (qualityPreset) => set({ qualityPreset }),
  requestCameraFit: () => set({ cameraFitNonce: get().cameraFitNonce + 1 }),
  recenter: () => {
    set({
      userInteractedCamera: false,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
  resetView: () => {
    set({
      cameraMode: "fullSystem",
      focusBodyName: null,
      userInteractedCamera: false,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
  rebuildModel: () => {
    const { telemetry, scrubEnabled, scrubUniversalTime } = get();
    const model = rebuild(telemetry, scrubEnabled, scrubUniversalTime);
    set({
      model,
      focusBodyName: focusForMode(model, get().cameraMode),
    });
  },
  getQuality: () => getQualitySettings(get().qualityPreset),
}));
