export interface Vector3 {
  x: number;
  y: number;
  z: number;
}

export interface PlacementSample {
  sampleRole?: string;
  targetBody?: string;
  sampleUniversalTimeSeconds?: number;
  positionRootRelativeMeters?: Vector3;
  sampleSource?: string;
  sampleWarning?: string;
}

export interface EphemerisSample {
  targetBody?: string;
  sampleRole?: string;
  sampleUniversalTimeSeconds?: number;
  positionRootRelativeMeters?: Vector3;
  velocityRootRelativeMetersPerSecond?: Vector3;
  sampleSource?: string;
  sampleWarning?: string;
}

export interface OrbitElements {
  classification?: string;
  referenceBody?: string;
  referenceFrame?: string;
  semiMajorAxisMeters?: number;
  semiLatusRectumMeters?: number;
  eccentricity?: number;
  inclinationDegrees?: number;
  longitudeOfAscendingNodeDegrees?: number;
  argumentOfPeriapsisDegrees?: number;
  trueAnomalyDegrees?: number;
  referenceBodyRadiusMeters?: number;
  apoapsisRadiusMeters?: number;
  periapsisRadiusMeters?: number;
  sphereOfInfluenceMeters?: number;
  patchStartUniversalTimeSeconds?: number;
  patchEndUniversalTimeSeconds?: number;
}

export interface OrbitPatch extends OrbitElements {
  patchIndex?: number;
  isActivePatch?: boolean;
  patchStartTransition?: string;
  patchEndTransition?: string;
  previousPatchReferenceBody?: string;
  nextPatchReferenceBody?: string;
  encounterBody?: string;
  encounterLevel?: string;
  closestEncounterUniversalTimeSeconds?: number;
  closestApproachMeters?: number;
  captureWarning?: string;
  referenceBodyPositionRootRelativeMeters?: Vector3;
  referenceBodyPositionSampleUniversalTimeSeconds?: number;
  patchPlacementMode?: string;
  patchPlacementWarning?: string;
  placementSamples?: PlacementSample[];
}

export interface CelestialBody {
  name?: string;
  parentBody?: string;
  radiusMeters?: number;
  sphereOfInfluenceMeters?: number;
  gravParameter?: number;
  hasAtmosphere?: boolean;
  atmosphereDepthMeters?: number;
  orbitReferenceBody?: string;
  positionReferenceFrame?: string;
  positionSampleUniversalTimeSeconds?: number;
  positionRootRelativeMeters?: Vector3;
  velocityReferenceFrame?: string;
  velocityRootRelativeMetersPerSecond?: Vector3;
}

export interface VesselRootPathSample {
  sampleUniversalTimeSeconds?: number;
  positionRootRelativeMeters?: Vector3;
}

export interface ActiveVessel {
  id?: string;
  name?: string;
  type?: string;
  situation?: string;
  mainBody?: string;
  positionRootRelativeMeters?: Vector3;
  velocityRootRelativeMetersPerSecond?: Vector3;
  rootPathSamples?: VesselRootPathSample[];
}

export interface BodyOrbitPathSample {
  sampleUniversalTimeSeconds?: number;
  positionRootRelativeMeters?: Vector3;
}

export interface BodyOrbitPath {
  bodyName?: string;
  referenceBody?: string;
  classification?: string;
  captureWarning?: string;
  samples?: BodyOrbitPathSample[];
}

export interface TelemetrySnapshot {
  schemaVersion?: number;
  valid?: boolean;
  status?: string;
  gameUniversalTimeSeconds?: number;
  rootFrameName?: string;
  rootBody?: string;
  rootFrameOriginBody?: string;
  rootFrameCapturedAtUniversalTimeSeconds?: number;
  rootFrameWarning?: string;
  patchChainStatus?: string;
  ephemerisCaptureStatus?: string;
  ephemerisValidationResidualMeters?: number;
  bodies?: CelestialBody[];
  activeVessel?: ActiveVessel;
  orbit?: OrbitElements;
  orbitPatches?: OrbitPatch[];
  ephemerisSamples?: EphemerisSample[];
  bodyOrbitPaths?: BodyOrbitPath[];
  bodyOrbitCaptureStatus?: string;
}
