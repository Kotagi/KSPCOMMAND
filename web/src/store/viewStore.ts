import { create } from "zustand";
import type { TelemetrySnapshot, Vector3 } from "../telemetry/schema-v6";
import {
  buildSolarSystemModel,
  type SolarSystemModel,
} from "../model/buildSolarSystemModel";
import type { QualityPreset } from "../settings/qualityStore";
import type { SelectionDetail } from "../selection/types";
import type { MoonVisibilityReason } from "../scene/moonVisibility";

export interface MoonLodDebugState {
  reason: MoonVisibilityReason;
  activeHostPlanet: string | null;
  visibleCount: number;
  label: string;
}

export type CameraMode =
  | "fullSystem"
  | "activeVessel"
  | "currentReferenceBody"
  | "encounterBody"
  | "route"
  | "bodyFocus";

export type SolarRenderMode = "3d" | "2d";

interface ViewState {
  telemetry: TelemetrySnapshot | null;
  model: SolarSystemModel | null;
  cameraMode: CameraMode;
  displayScale: number;
  focusBodyName: string | null;
  /** Camera mode to restore when leaving body focus (click unfocus). */
  cameraModeBeforeBodyFocus: CameraMode | null;
  scrubEnabled: boolean;
  scrubUniversalTime: number | null;
  solarRenderMode: SolarRenderMode;
  selectedObjectId: string | null;
  selectionDetail: SelectionDetail | null;
  hoverObjectId: string | null;
  userInteractedCamera: boolean;
  qualityPreset: QualityPreset;
  cameraFitNonce: number;
  solarFullscreen: boolean;
  vesselDisplayPosition: Vector3 | null;
  vesselTargetPosition: Vector3 | null;
  moonLodDebug: MoonLodDebugState | null;
  setMoonLodDebug: (debug: MoonLodDebugState | null) => void;
  setTelemetry: (telemetry: TelemetrySnapshot | null) => void;
  setCameraMode: (mode: CameraMode) => void;
  setDisplayScale: (scale: number) => void;
  setFocusBodyName: (name: string | null) => void;
  focusOnBody: (bodyName: string) => void;
  unfocusBody: () => void;
  setScrubEnabled: (enabled: boolean) => void;
  setScrubUniversalTime: (ut: number | null) => void;
  setSolarRenderMode: (mode: SolarRenderMode) => void;
  setSelectedObjectId: (id: string | null) => void;
  setSelectionDetail: (detail: SelectionDetail | null) => void;
  setHoverObjectId: (id: string | null) => void;
  setUserInteractedCamera: (value: boolean) => void;
  setQualityPreset: (preset: QualityPreset) => void;
  requestCameraFit: () => void;
  setSolarFullscreen: (enabled: boolean) => void;
  toggleSolarFullscreen: () => void;
  recenter: () => void;
  resetView: () => void;
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

function focusForMode(
  model: SolarSystemModel | null,
  mode: CameraMode,
  bodyFocusTarget: string | null = null,
): string | null {
  if (!model) {
    return null;
  }
  if (mode === "bodyFocus" && bodyFocusTarget) {
    return bodyFocusTarget;
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

function resolveFocusBodyName(
  model: SolarSystemModel | null,
  cameraMode: CameraMode,
  bodyFocusTarget: string | null,
): string | null {
  return focusForMode(model, cameraMode, bodyFocusTarget);
}

export const useViewStore = create<ViewState>((set, get) => ({
  telemetry: null,
  model: null,
  cameraMode: "fullSystem",
  displayScale: 1e-9,
  focusBodyName: null,
  cameraModeBeforeBodyFocus: null,
  scrubEnabled: false,
  scrubUniversalTime: null,
  solarRenderMode: "3d",
  selectedObjectId: null,
  selectionDetail: null,
  hoverObjectId: null,
  userInteractedCamera: false,
  qualityPreset: "medium",
  cameraFitNonce: 0,
  solarFullscreen: false,
  vesselDisplayPosition: null,
  vesselTargetPosition: null,
  moonLodDebug: null,
  setMoonLodDebug: (moonLodDebug) => set({ moonLodDebug }),
  setTelemetry: (telemetry) => {
    const { scrubEnabled, scrubUniversalTime, vesselDisplayPosition } = get();
    const model = rebuild(telemetry, scrubEnabled, scrubUniversalTime);
    const nextTarget = model?.vesselPosition ?? null;
    set({
      telemetry,
      model,
      vesselTargetPosition: nextTarget,
      vesselDisplayPosition: vesselDisplayPosition ?? nextTarget,
      focusBodyName: resolveFocusBodyName(
        model,
        get().cameraMode,
        get().cameraMode === "bodyFocus" ? get().focusBodyName : null,
      ),
    });
  },
  setCameraMode: (cameraMode) => {
    const { model } = get();
    set({
      cameraMode,
      cameraModeBeforeBodyFocus: null,
      focusBodyName: resolveFocusBodyName(model, cameraMode, null),
      userInteractedCamera: false,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
  setDisplayScale: (displayScale) => set({ displayScale }),
  setFocusBodyName: (focusBodyName) => set({ focusBodyName }),
  focusOnBody: (bodyName) => {
    const { model, cameraMode, cameraModeBeforeBodyFocus } = get();
    if (!model?.bodies.some((b) => b.body.name === bodyName)) {
      return;
    }
    const restoreMode =
      cameraMode !== "bodyFocus" ? cameraMode : cameraModeBeforeBodyFocus;
    set({
      cameraMode: "bodyFocus",
      cameraModeBeforeBodyFocus: restoreMode ?? "fullSystem",
      focusBodyName: bodyName,
      userInteractedCamera: false,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
  unfocusBody: () => {
    const { model, cameraModeBeforeBodyFocus } = get();
    const restore = cameraModeBeforeBodyFocus ?? "fullSystem";
    set({
      cameraMode: restore,
      cameraModeBeforeBodyFocus: null,
      focusBodyName: resolveFocusBodyName(model, restore, null),
      userInteractedCamera: false,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
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
  setSolarFullscreen: (solarFullscreen) => {
    if (!solarFullscreen && document.fullscreenElement) {
      void document.exitFullscreen().catch(() => undefined);
    }
    set({
      solarFullscreen,
      userInteractedCamera: solarFullscreen ? false : get().userInteractedCamera,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
  toggleSolarFullscreen: () => {
    get().setSolarFullscreen(!get().solarFullscreen);
  },
  recenter: () => {
    set({
      userInteractedCamera: false,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
  resetView: () => {
    set({
      cameraMode: "fullSystem",
      cameraModeBeforeBodyFocus: null,
      focusBodyName: null,
      userInteractedCamera: false,
      cameraFitNonce: get().cameraFitNonce + 1,
    });
  },
  rebuildModel: () => {
    const { telemetry, scrubEnabled, scrubUniversalTime, cameraMode, focusBodyName } =
      get();
    const model = rebuild(telemetry, scrubEnabled, scrubUniversalTime);
    const bodyFocusTarget = cameraMode === "bodyFocus" ? focusBodyName : null;
    set({
      model,
      focusBodyName: resolveFocusBodyName(model, cameraMode, bodyFocusTarget),
    });
  },
}));
