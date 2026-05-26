import type { Vector3 } from "../telemetry/schema-v6";
import {
  kspRootQuaternionFromAxisAngle,
  kspRootQuaternionObliquityTilt,
  kspRootQuaternionSpinAboutNorth,
  normalizeKspRootQuaternion,
  type KspRootQuaternion,
} from "../coords/kspBodyOrientation";
import {
  readPlanetBodyOrientation,
  resolvePlanetBodyOrientationAtUt,
  type CelestialBodyWithOrientation,
} from "../map-v3/elements/planetBody/planetBodyOrientationFields";

export type LabOrientationPreset =
  | "none"
  | "identity"
  | "tilt-x-45"
  | "tilt-x-90"
  | "obliquity-23"
  | "spin-y"
  | "tilt-and-spin"
  | "custom"
  | "telemetry";

export interface LabOrientationConfig {
  preset: LabOrientationPreset;
  orientationKsp: KspRootQuaternion;
  angularVelocityKsp?: Vector3;
  extrapolateSpin: boolean;
  sourceLabel: string;
}

const KERBIN_SIDereal_PERIOD = 21_600;
const KERBIN_ANGULAR_SPEED = (2 * Math.PI) / KERBIN_SIDereal_PERIOD;

function parseNumber(params: URLSearchParams, key: string): number | undefined {
  const raw = params.get(key);
  if (raw == null || raw === "") {
    return undefined;
  }
  const n = Number(raw);
  return Number.isFinite(n) ? n : undefined;
}

function parseCustomQuaternion(params: URLSearchParams): KspRootQuaternion | undefined {
  const x = parseNumber(params, "qx");
  const y = parseNumber(params, "qy");
  const z = parseNumber(params, "qz");
  const w = parseNumber(params, "qw");
  if (x == null || y == null || z == null || w == null) {
    return undefined;
  }
  return normalizeKspRootQuaternion({ x, y, z, w });
}

function presetOrientation(
  preset: LabOrientationPreset,
  params: URLSearchParams,
): {
  orientation: KspRootQuaternion;
  angularVelocity?: Vector3;
  extrapolate?: boolean;
  label: string;
} {
  switch (preset) {
    case "identity":
      return {
        orientation: { x: 0, y: 0, z: 0, w: 1 },
        label: "identity (KSP root)",
      };
    case "tilt-x-45":
      return {
        orientation: kspRootQuaternionFromAxisAngle(
          { x: 1, y: 0, z: 0 },
          Math.PI / 4,
        ),
        label: "45° tilt about KSP +X (obliquity POC)",
      };
    case "tilt-x-90":
      return {
        orientation: kspRootQuaternionFromAxisAngle(
          { x: 1, y: 0, z: 0 },
          Math.PI / 2,
        ),
        label: "90° tilt about KSP +X — north pole toward +Z",
      };
    case "obliquity-23":
      return {
        orientation: kspRootQuaternionObliquityTilt(
          (23.5 * Math.PI) / 180,
          0,
        ),
        label: "23.5° obliquity (Earth-like) about KSP +X",
      };
    case "spin-y": {
      const angle = parseNumber(params, "angle") ?? 0;
      return {
        orientation: kspRootQuaternionSpinAboutNorth(angle),
        angularVelocity: { x: 0, y: KERBIN_ANGULAR_SPEED, z: 0 },
        extrapolate: true,
        label: `spin about KSP north (+Y), angle=${angle.toFixed(2)} rad`,
      };
    }
    case "tilt-and-spin":
      return {
        orientation: kspRootQuaternionObliquityTilt(Math.PI / 4, 0),
        angularVelocity: { x: 0, y: KERBIN_ANGULAR_SPEED, z: 0 },
        extrapolate: true,
        label: "45° tilt + Kerbin sidereal spin",
      };
    case "none":
    default:
      return {
        orientation: { x: 0, y: 0, z: 0, w: 1 },
        extrapolate: false,
        label: "none (identity)",
      };
  }
}

export function parseLabOrientationFromSearch(
  search: string,
): LabOrientationConfig {
  const params = new URLSearchParams(search);
  const presetParam = (params.get("orientation") ??
    params.get("orient") ??
    "none") as LabOrientationPreset;
  const spinFlag = params.get("spin") === "1" || params.get("spin") === "true";

  if (presetParam === "custom") {
    const custom = parseCustomQuaternion(params);
    if (custom) {
      const avx = parseNumber(params, "avx") ?? 0;
      const avy = parseNumber(params, "avy") ?? 0;
      const avz = parseNumber(params, "avz") ?? 0;
      const hasAv = avx !== 0 || avy !== 0 || avz !== 0;
      return {
        preset: "custom",
        orientationKsp: custom,
        angularVelocityKsp: hasAv ? { x: avx, y: avy, z: avz } : undefined,
        extrapolateSpin: spinFlag || hasAv,
        sourceLabel: `custom q=(${custom.x.toFixed(3)},${custom.y.toFixed(3)},${custom.z.toFixed(3)},${custom.w.toFixed(3)})`,
      };
    }
  }

  const preset = presetParam === "custom" ? "none" : presetParam;
  const built = presetOrientation(preset, params);
  return {
    preset,
    orientationKsp: built.orientation,
    angularVelocityKsp: built.angularVelocity,
    extrapolateSpin: spinFlag || built.extrapolate === true,
    sourceLabel: built.label,
  };
}

export async function fetchLabOrientationFromTelemetry(
  bodyName: string,
): Promise<LabOrientationConfig | { error: string }> {
  try {
    const res = await fetch("/api/telemetry");
    if (!res.ok) {
      return { error: `telemetry HTTP ${res.status}` };
    }
    const data = (await res.json()) as {
      bodies?: CelestialBodyWithOrientation[];
      gameUniversalTimeSeconds?: number;
    };
    const body = data.bodies?.find((b) => b.name === bodyName);
    if (!body) {
      return { error: `body "${bodyName}" not in telemetry` };
    }
    const gameUt =
      data.gameUniversalTimeSeconds ??
      body.bodyOrientationSampleUniversalTimeSeconds;
    const orientation =
      (gameUt != null
        ? resolvePlanetBodyOrientationAtUt(body, gameUt)
        : undefined) ?? readPlanetBodyOrientation(body);
    if (!orientation) {
      return {
        error:
          "bodyOrientationRootRelative missing — DLL Phase 3.4.2 not shipped yet; use ?orientation=tilt-x-90 or ?orientation=tilt-and-spin",
      };
    }
    return {
      preset: "telemetry",
      orientationKsp: orientation,
      extrapolateSpin: false,
      sourceLabel: `telemetry ${bodyName} @ UT ${gameUt ?? "?"}`,
    };
  } catch (e) {
    return { error: e instanceof Error ? e.message : String(e) };
  }
}

export function labOrientationExampleUrls(pagePath: string): string[] {
  const path = pagePath.split("?")[0];
  return [
    `${path}?orientation=tilt-x-90`,
    `${path}?orientation=tilt-and-spin&spin=1`,
    `${path}?orientation=obliquity-23`,
    `${path}?orientation=custom&qx=0&qy=0&qz=0&qw=1&avy=0.00029&spin=1`,
    `${path}?orientation=telemetry&body=Kerbin`,
  ];
}
