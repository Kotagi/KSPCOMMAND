using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspWebMap
{
    public sealed class TelemetrySnapshotService : IKspWebMapService
    {
        private readonly GameObject _host;
        private readonly TelemetryStore _store;
        private TelemetrySnapshotComponent _component;

        public TelemetrySnapshotService(GameObject host, TelemetryStore store)
        {
            _host = host;
            _store = store;
        }

        public string Name
        {
            get { return "Telemetry Snapshot"; }
        }

        public void Start()
        {
            if (_component == null)
            {
                _component = _host.AddComponent<TelemetrySnapshotComponent>();
                _component.Initialize(_store);
            }
        }

        public void Stop()
        {
            if (_component != null)
            {
                UnityEngine.Object.Destroy(_component);
                _component = null;
            }
        }

        private sealed class TelemetrySnapshotComponent : MonoBehaviour
        {
            private const float CaptureIntervalSeconds = 0.2f;
            private const string LogPrefix = "[KspWebMap]";
            private const int MaxOrbitPatchCount = 8;
            private const int VesselRootPathSampleCount = 32;
            private const int BodyOrbitPathSampleCount = 128;
            private const int MaxBodyOrbitPathCount = 48;
            private const double EphemerisValidationToleranceSeconds = 1d;
            private const double VesselPathLiveToleranceMeters = 1000d;
            private const double VesselPathUniversalTimeToleranceSeconds = 1d;
            private const string RootFrameName = "solarSystemRootCenteredInertial";
            private TelemetryStore _store;
            private float _nextCaptureTime;

            public void Initialize(TelemetryStore store)
            {
                _store = store;
                CaptureAndPublish();
            }

            private void LateUpdate()
            {
                if (_store == null || Time.realtimeSinceStartup < _nextCaptureTime)
                {
                    return;
                }

                CaptureAndPublish();
                _nextCaptureTime = Time.realtimeSinceStartup + CaptureIntervalSeconds;
            }

            private void CaptureAndPublish()
            {
                try
                {
                    _store.Publish(CaptureSnapshot());
                }
                catch (Exception ex)
                {
                    long snapshotId = _store.NextSnapshotId();
                    _store.Publish(TelemetrySnapshot.CreateInvalid(snapshotId, "Telemetry capture failed: " + ex.Message));
                    Debug.LogError(string.Format("{0} Telemetry capture failed: {1}", LogPrefix, ex));
                }
            }

            private TelemetrySnapshot CaptureSnapshot()
            {
                long snapshotId = _store.NextSnapshotId();
                DateTime captureStartedUtc = DateTime.UtcNow;
                double universalTime = GetUniversalTimeSeconds();
                CelestialBody rootBody = FindRootBody();
                string rootFrameWarning = rootBody == null ? "Root body could not be identified." : null;
                RootRelativePositionResolver.ResetCalibration();
                RootRelativePositionResolver.EnsureCalibrated(rootBody, universalTime);
                CelestialBodySnapshot[] bodies = CaptureBodies(rootBody, universalTime);

                Vessel vessel = FlightGlobals.ActiveVessel;

                if (vessel == null)
                {
                    return new TelemetrySnapshot
                    {
                        SchemaVersion = TelemetrySnapshot.CurrentSchemaVersion,
                        SnapshotId = snapshotId,
                        CapturedAtUtc = DateTime.UtcNow,
                        GameUniversalTimeSeconds = universalTime,
                        RootFrameName = RootFrameName,
                        RootBody = rootBody != null ? rootBody.bodyName : null,
                        RootFrameOriginBody = rootBody != null ? rootBody.bodyName : null,
                        RootFrameCapturedAtUniversalTimeSeconds = universalTime,
                        RootFrameWarning = rootFrameWarning,
                        Scene = GetSceneName(),
                        Valid = false,
                        Status = "No active vessel.",
                        ActiveVessel = null,
                        Orbit = null,
                        OrbitPatches = new OrbitPatchSnapshot[0],
                        PatchChainStatus = "no-patches",
                        Bodies = bodies,
                        EphemerisSamples = new EphemerisSampleSnapshot[0],
                        EphemerisCaptureStatus = rootBody != null ? "partial" : "unsupported",
                        EphemerisValidationResidualMeters = double.NaN,
                        BodyOrbitPropagationResidualMeters = double.NaN,
                        BodyOrbitPaths = CaptureBodyOrbitPaths(rootBody, universalTime),
                        BodyOrbitCaptureStatus = rootBody != null ? "ok" : "unsupported"
                    };
                }

                Orbit orbit = vessel.orbit;
                string patchChainStatus;
                OrbitPatchSnapshot[] orbitPatches = CaptureOrbitPatches(vessel, orbit, universalTime, rootBody, out patchChainStatus);
                EphemerisSampleSnapshot[] ephemerisSamples = CaptureEphemerisSamples(orbitPatches, rootBody, universalTime);
                string ephemerisCaptureStatus = DetermineEphemerisCaptureStatus(orbitPatches, ephemerisSamples);
                BodyOrbitPathSnapshot[] bodyOrbitPaths = CaptureBodyOrbitPaths(rootBody, universalTime);
                double bodyOrbitPropagationResidual;
                double bodyOrbitFlipPropagationResidual;
                double bodyOrbitSampleResidual;
                double bodyOrbitAnalyticResidual;
                double bodyOrbitPeriodClosureResidual;
                double ephemerisLivePropagationResidual;
                double iconTrailSample0Residual;
                PositionValidationSnapshot positionValidation;
                double ephemerisValidationResidual = ValidateEphemerisPropagation(
                    rootBody,
                    universalTime,
                    bodies,
                    bodyOrbitPaths,
                    out iconTrailSample0Residual,
                    out bodyOrbitPropagationResidual,
                    out bodyOrbitFlipPropagationResidual,
                    out bodyOrbitSampleResidual,
                    out bodyOrbitAnalyticResidual,
                    out bodyOrbitPeriodClosureResidual,
                    out ephemerisLivePropagationResidual,
                    out positionValidation);

                DiagnosticsLogger.LogBodyOrbitWarnings(bodyOrbitPaths, universalTime);

                FrameDiagnosticsSnapshot frameDiagnostics = BuildFrameDiagnostics(
                    captureStartedUtc,
                    bodies != null ? bodies.Length : 0,
                    bodyOrbitPaths != null ? bodyOrbitPaths.Length : 0);

                return new TelemetrySnapshot
                {
                    SchemaVersion = TelemetrySnapshot.CurrentSchemaVersion,
                    SnapshotId = snapshotId,
                    CapturedAtUtc = DateTime.UtcNow,
                    GameUniversalTimeSeconds = universalTime,
                    RootFrameName = RootFrameName,
                    RootBody = rootBody != null ? rootBody.bodyName : null,
                    RootFrameOriginBody = rootBody != null ? rootBody.bodyName : null,
                    RootFrameCapturedAtUniversalTimeSeconds = universalTime,
                    RootFrameWarning = rootFrameWarning,
                    Scene = GetSceneName(),
                    Valid = true,
                    Status = "ok",
                    ActiveVessel = CaptureActiveVessel(vessel, rootBody, orbitPatches, universalTime),
                    Orbit = CaptureOrbit(vessel, orbit, universalTime),
                    OrbitPatches = orbitPatches,
                    PatchChainStatus = patchChainStatus,
                    Bodies = bodies,
                    EphemerisSamples = ephemerisSamples,
                    EphemerisCaptureStatus = ephemerisCaptureStatus,
                    EphemerisValidationResidualMeters = ephemerisValidationResidual,
                    EphemerisLivePropagationResidualMeters = ephemerisLivePropagationResidual,
                    IconTrailSample0ResidualMeters = iconTrailSample0Residual,
                    BodyOrbitPropagationResidualMeters = bodyOrbitFlipPropagationResidual,
                    BodyOrbitFlipPropagationResidualMeters = bodyOrbitFlipPropagationResidual,
                    BodyOrbitSampleResidualMeters = bodyOrbitSampleResidual,
                    BodyOrbitAnalyticResidualMeters = bodyOrbitAnalyticResidual,
                    BodyOrbitPeriodClosureResidualMeters = bodyOrbitPeriodClosureResidual,
                    BodyOrbitPaths = bodyOrbitPaths,
                    BodyOrbitCaptureStatus = DetermineBodyOrbitCaptureStatus(rootBody),
                    FrameDiagnostics = frameDiagnostics,
                    PositionValidation = positionValidation
                };
            }

            private static FrameDiagnosticsSnapshot BuildFrameDiagnostics(
                DateTime captureStartedUtc,
                int bodiesCaptured,
                int bodyPathsCaptured)
            {
                return new FrameDiagnosticsSnapshot
                {
                    ResolverVersion = RootRelativePositionResolver.ResolverVersion,
                    OrbitOffsetMode = RootRelativePositionResolver.OrbitOffsetModeName,
                    VesselOffsetMode = RootRelativePositionResolver.VesselOffsetModeName,
                    PluginBuildUtc = captureStartedUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    CaptureDurationMs = (DateTime.UtcNow - captureStartedUtc).TotalMilliseconds,
                    BodiesCaptured = bodiesCaptured,
                    BodyPathsCaptured = bodyPathsCaptured
                };
            }

            private static ActiveVesselSnapshot CaptureActiveVessel(Vessel vessel, CelestialBody rootBody, OrbitPatchSnapshot[] orbitPatches, double universalTime)
            {
                CelestialBody mainBody = vessel.mainBody;
                Orbit orbit = vessel.orbit;
                CelestialBody referenceBody = orbit != null ? orbit.referenceBody : mainBody;

                return new ActiveVesselSnapshot
                {
                    Id = vessel.id.ToString(),
                    Name = vessel.vesselName,
                    Type = vessel.vesselType.ToString(),
                    Situation = vessel.situation.ToString(),
                    MainBody = mainBody != null ? mainBody.bodyName : null,
                    AltitudeMeters = vessel.altitude,
                    RadarAltitudeMeters = vessel.radarAltitude,
                    SurfaceSpeedMetersPerSecond = vessel.srfSpeed,
                    OrbitalSpeedMetersPerSecond = vessel.obt_speed,
                    PositionReferenceFrame = referenceBody != null ? "orbitReferenceBodyCenteredInertial" : null,
                    PositionRelativeToReferenceBodyMeters = CaptureRelativePosition(vessel, referenceBody),
                    VelocityRelativeToReferenceBodyMetersPerSecond = CaptureOrbitalVelocity(vessel),
                    RootPositionReferenceFrame = rootBody != null ? RootFrameName : null,
                    PositionRootRelativeMeters = CaptureVesselRootPosition(vessel, rootBody),
                    RootVelocityReferenceFrame = rootBody != null ? RootFrameName : null,
                    VelocityRootRelativeMetersPerSecond = CaptureVesselRootVelocity(vessel, referenceBody, rootBody),
                    RootPathSamples = CaptureVesselRootPathSamples(vessel, orbit, rootBody, orbitPatches, universalTime)
                };
            }

            private static OrbitSnapshot CaptureOrbit(Vessel vessel, Orbit orbit, double universalTime)
            {
                if (orbit == null)
                {
                    return null;
                }

                CelestialBody referenceBody = orbit.referenceBody;
                double referenceBodyRadius = referenceBody != null ? referenceBody.Radius : 0d;
                double apoapsisRadius = referenceBodyRadius + orbit.ApA;
                double periapsisRadius = referenceBodyRadius + orbit.PeA;

                return new OrbitSnapshot
                {
                    Classification = ClassifyOrbit(vessel, orbit),
                    ReferenceBody = referenceBody != null ? referenceBody.bodyName : null,
                    ReferenceFrame = referenceBody != null ? "orbitReferenceBodyCenteredInertial" : null,
                    ReferenceBodyRadiusMeters = referenceBodyRadius,
                    SphereOfInfluenceMeters = referenceBody != null ? referenceBody.sphereOfInfluence : double.NaN,
                    ApoapsisMeters = orbit.ApA,
                    PeriapsisMeters = orbit.PeA,
                    ApoapsisRadiusMeters = apoapsisRadius,
                    PeriapsisRadiusMeters = periapsisRadius,
                    SemiMajorAxisMeters = orbit.semiMajorAxis,
                    SemiLatusRectumMeters = CalculateSemiLatusRectum(orbit.semiMajorAxis, orbit.eccentricity),
                    Eccentricity = orbit.eccentricity,
                    InclinationDegrees = orbit.inclination,
                    LongitudeOfAscendingNodeDegrees = orbit.LAN,
                    ArgumentOfPeriapsisDegrees = orbit.argumentOfPeriapsis,
                    TrueAnomalyDegrees = orbit.trueAnomaly,
                    MeanAnomalyRadians = orbit.meanAnomaly,
                    EpochUniversalTimeSeconds = orbit.epoch,
                    PatchStartUniversalTimeSeconds = orbit.StartUT,
                    PatchEndUniversalTimeSeconds = orbit.EndUT,
                    PatchStartTransition = orbit.patchStartTransition.ToString(),
                    PatchEndTransition = orbit.patchEndTransition.ToString(),
                    ActivePatch = orbit.activePatch,
                    NextPatchReferenceBody = GetNextPatchReferenceBody(orbit),
                    TimeToTransitionSeconds = CalculateTimeToTransition(orbit.EndUT, universalTime),
                    PeriodSeconds = orbit.period,
                    TimeToApoapsisSeconds = orbit.timeToAp,
                    TimeToPeriapsisSeconds = orbit.timeToPe
                };
            }

            private static OrbitPatchSnapshot[] CaptureOrbitPatches(Vessel vessel, Orbit orbit, double universalTime, CelestialBody rootBody, out string patchChainStatus)
            {
                List<OrbitPatchSnapshot> patches = new List<OrbitPatchSnapshot>();
                HashSet<Orbit> visited = new HashSet<Orbit>();
                Orbit current = orbit;
                string status = "ok";

                if (current == null)
                {
                    patchChainStatus = "no-patches";
                    return patches.ToArray();
                }

                for (int i = 0; i < MaxOrbitPatchCount && current != null; i++)
                {
                    if (visited.Contains(current))
                    {
                        status = "truncated";
                        if (patches.Count > 0)
                        {
                            patches[patches.Count - 1].CaptureWarning = "Patch chain stopped after repeated patch reference.";
                        }

                        break;
                    }

                    visited.Add(current);
                    patches.Add(CaptureOrbitPatch(vessel, current, i, universalTime, rootBody));

                    Orbit next = current.nextPatch;

                    if (next == null)
                    {
                        break;
                    }

                    if (!next.activePatch)
                    {
                        if (current.patchEndTransition.ToString() == "FINAL")
                        {
                            break;
                        }

                        status = "incomplete";
                        patches[patches.Count - 1].CaptureWarning = "Patch chain stopped at inactive nextPatch.";
                        break;
                    }

                    current = next;
                }

                if (patches.Count >= MaxOrbitPatchCount && current != null && current.nextPatch != null)
                {
                    status = "truncated";
                    patches[patches.Count - 1].CaptureWarning = "Patch chain reached the maximum captured patch count.";
                }

                patchChainStatus = patches.Count == 0 ? "no-patches" : status;
                return patches.ToArray();
            }

            private static OrbitPatchSnapshot CaptureOrbitPatch(Vessel vessel, Orbit orbit, int patchIndex, double universalTime, CelestialBody rootBody)
            {
                CelestialBody referenceBody = orbit.referenceBody;
                double referenceBodyRadius = referenceBody != null ? referenceBody.Radius : 0d;
                bool hasRootAnchor = referenceBody != null && rootBody != null;
                string encounterBodyName = GetEncounterBody(orbit);
                CelestialBody encounterBody = FindBodyByName(encounterBodyName);
                List<PlacementSampleSnapshot> placementSamples = new List<PlacementSampleSnapshot>();

                PlacementSampleSnapshot startSample = CapturePlacementSample(
                    referenceBody,
                    rootBody,
                    orbit.StartUT,
                    "patchStart",
                    orbit.StartUT,
                    orbit.EndUT,
                    universalTime);

                PlacementSampleSnapshot endSample = CapturePlacementSample(
                    referenceBody,
                    rootBody,
                    orbit.EndUT,
                    "patchEnd",
                    orbit.StartUT,
                    orbit.EndUT,
                    universalTime);

                if (startSample != null)
                {
                    placementSamples.Add(startSample);
                }

                if (endSample != null)
                {
                    placementSamples.Add(endSample);
                }

                PlacementSampleSnapshot encounterSample = null;

                if (encounterBody != null)
                {
                    double encounterUniversalTime = orbit.closestTgtApprUT;
                    string encounterTimeWarning = null;

                    if (!IsValidEncounterUniversalTime(encounterUniversalTime))
                    {
                        if (IsValidUniversalTime(orbit.EndUT)
                            && IsUniversalTimeWithinPatchBounds(orbit.EndUT, orbit.StartUT, orbit.EndUT))
                        {
                            encounterUniversalTime = orbit.EndUT;
                            encounterTimeWarning = "closestEncounterUniversalTimeSeconds unavailable; encounter sample uses patchEndUniversalTimeSeconds.";
                        }
                    }

                    if (IsValidUniversalTime(encounterUniversalTime))
                    {
                        encounterSample = CapturePlacementSample(
                            encounterBody,
                            rootBody,
                            encounterUniversalTime,
                            "encounter",
                            orbit.StartUT,
                            orbit.EndUT,
                            universalTime);

                        if (encounterSample != null)
                        {
                            if (encounterTimeWarning != null)
                            {
                                encounterSample.SampleWarning = string.IsNullOrEmpty(encounterSample.SampleWarning)
                                    ? encounterTimeWarning
                                    : encounterSample.SampleWarning + " " + encounterTimeWarning;
                            }

                            placementSamples.Add(encounterSample);
                        }
                    }
                }

                Vector3Snapshot anchorPosition = startSample != null
                    ? startSample.PositionRootRelativeMeters
                    : CaptureBodyRootPosition(referenceBody, rootBody, universalTime);

                double anchorSampleUt = startSample != null
                    ? startSample.SampleUniversalTimeSeconds
                    : universalTime;

                string placementMode;
                string placementWarning;

                if (!hasRootAnchor)
                {
                    placementMode = null;
                    placementWarning = "Patch reference body root-frame anchor is unavailable.";
                }
                else if (startSample != null && endSample != null)
                {
                    placementMode = encounterBody != null && encounterSample == null
                        ? "multiSampleEphemerisPartial"
                        : "multiSampleEphemeris";
                    placementWarning = encounterBody != null && encounterSample == null
                        ? "Encounter body position at closest-approach UT could not be sampled; route uses start/end anchors only."
                        : null;
                }
                else
                {
                    placementMode = "currentReferenceBodyPosition";
                    placementWarning = "Ephemeris sampling failed for patch boundaries; using current reference-body position.";
                }

                return new OrbitPatchSnapshot
                {
                    PatchIndex = patchIndex,
                    IsActivePatch = patchIndex == 0,
                    Classification = patchIndex == 0 ? ClassifyOrbit(vessel, orbit) : ClassifyPatchOrbit(orbit),
                    ReferenceBody = referenceBody != null ? referenceBody.bodyName : null,
                    ReferenceFrame = referenceBody != null ? "orbitReferenceBodyCenteredInertial" : null,
                    ReferenceBodyPositionRootRelativeMeters = anchorPosition,
                    ReferenceBodyPositionSampleUniversalTimeSeconds = anchorSampleUt,
                    PatchPlacementMode = placementMode,
                    PatchPlacementWarning = placementWarning,
                    PlacementSamples = placementSamples.ToArray(),
                    ReferenceBodyRadiusMeters = referenceBodyRadius,
                    SphereOfInfluenceMeters = referenceBody != null ? referenceBody.sphereOfInfluence : double.NaN,
                    ApoapsisMeters = orbit.ApA,
                    PeriapsisMeters = orbit.PeA,
                    ApoapsisRadiusMeters = referenceBodyRadius + orbit.ApA,
                    PeriapsisRadiusMeters = referenceBodyRadius + orbit.PeA,
                    SemiMajorAxisMeters = orbit.semiMajorAxis,
                    SemiLatusRectumMeters = CalculateSemiLatusRectum(orbit.semiMajorAxis, orbit.eccentricity),
                    Eccentricity = orbit.eccentricity,
                    InclinationDegrees = orbit.inclination,
                    LongitudeOfAscendingNodeDegrees = orbit.LAN,
                    ArgumentOfPeriapsisDegrees = orbit.argumentOfPeriapsis,
                    TrueAnomalyDegrees = orbit.trueAnomaly,
                    MeanAnomalyRadians = orbit.meanAnomaly,
                    EpochUniversalTimeSeconds = orbit.epoch,
                    PatchStartUniversalTimeSeconds = orbit.StartUT,
                    PatchEndUniversalTimeSeconds = orbit.EndUT,
                    PatchStartTransition = orbit.patchStartTransition.ToString(),
                    PatchEndTransition = orbit.patchEndTransition.ToString(),
                    TimeToPatchStartSeconds = CalculateTimeToTransition(orbit.StartUT, universalTime),
                    TimeToPatchEndSeconds = CalculateTimeToTransition(orbit.EndUT, universalTime),
                    PreviousPatchReferenceBody = GetPatchReferenceBody(orbit.previousPatch),
                    NextPatchReferenceBody = GetNextPatchReferenceBody(orbit),
                    EncounterBody = encounterBodyName,
                    EncounterLevel = orbit.closestEncounterLevel.ToString(),
                    ClosestEncounterUniversalTimeSeconds = orbit.closestTgtApprUT,
                    ClosestApproachMeters = CaptureClosestApproachMeters(orbit, encounterBody),
                    CaptureWarning = null,
                    PeriodSeconds = orbit.period,
                    TimeToApoapsisSeconds = orbit.timeToAp,
                    TimeToPeriapsisSeconds = orbit.timeToPe
                };
            }

            private static string ClassifyOrbit(Vessel vessel, Orbit orbit)
            {
                if (vessel == null)
                {
                    return "NoVessel";
                }

                string situation = vessel.situation.ToString();

                if (situation == "PRELAUNCH")
                {
                    return "Prelaunch";
                }

                if (situation == "LANDED")
                {
                    return "Landed";
                }

                if (situation == "SPLASHED")
                {
                    return "Splashed";
                }

                if (orbit == null)
                {
                    return "Invalid";
                }

                if (orbit.eccentricity >= 1d)
                {
                    return "HyperbolicEscape";
                }

                CelestialBody referenceBody = orbit.referenceBody;

                if (referenceBody == null)
                {
                    return "Invalid";
                }

                if (orbit.PeA < 0d || orbit.PeR < referenceBody.Radius)
                {
                    return "Suborbital";
                }

                return "Elliptic";
            }

            private static string ClassifyPatchOrbit(Orbit orbit)
            {
                if (orbit == null)
                {
                    return "Invalid";
                }

                if (orbit.eccentricity >= 1d)
                {
                    return "HyperbolicEscape";
                }

                CelestialBody referenceBody = orbit.referenceBody;

                if (referenceBody == null)
                {
                    return "Invalid";
                }

                if (orbit.PeA < 0d || orbit.PeR < referenceBody.Radius)
                {
                    return "Suborbital";
                }

                return "Elliptic";
            }

            private static double CalculateSemiLatusRectum(double semiMajorAxis, double eccentricity)
            {
                if (double.IsNaN(semiMajorAxis) || double.IsInfinity(semiMajorAxis) || double.IsNaN(eccentricity) || double.IsInfinity(eccentricity))
                {
                    return double.NaN;
                }

                if (eccentricity < 1d)
                {
                    return semiMajorAxis * (1d - eccentricity * eccentricity);
                }

                if (eccentricity > 1d)
                {
                    return Math.Abs(semiMajorAxis) * (eccentricity * eccentricity - 1d);
                }

                return double.NaN;
            }

            private static double CalculateTimeToTransition(double endUniversalTime, double currentUniversalTime)
            {
                if (double.IsNaN(endUniversalTime) || double.IsInfinity(endUniversalTime) || double.IsNaN(currentUniversalTime) || double.IsInfinity(currentUniversalTime))
                {
                    return double.NaN;
                }

                return endUniversalTime - currentUniversalTime;
            }

            private static string GetNextPatchReferenceBody(Orbit orbit)
            {
                if (orbit == null || orbit.nextPatch == null || !orbit.nextPatch.activePatch || orbit.nextPatch.referenceBody == null)
                {
                    return null;
                }

                return orbit.nextPatch.referenceBody.bodyName;
            }

            private static string GetEncounterBody(Orbit orbit)
            {
                if (orbit == null)
                {
                    return null;
                }

                if (orbit.patchEndTransition.ToString() == "ENCOUNTER" && orbit.nextPatch != null && orbit.nextPatch.referenceBody != null)
                {
                    return orbit.nextPatch.referenceBody.bodyName;
                }

                if (orbit.closestEncounterLevel.ToString() == "NONE")
                {
                    return null;
                }

                return orbit.closestEncounterBody != null ? orbit.closestEncounterBody.bodyName : null;
            }

            private static string GetPatchReferenceBody(Orbit orbit)
            {
                if (orbit == null || orbit.referenceBody == null)
                {
                    return null;
                }

                return orbit.referenceBody.bodyName;
            }

            private static Vector3Snapshot CaptureRelativePosition(Vessel vessel, CelestialBody referenceBody)
            {
                if (vessel == null || referenceBody == null)
                {
                    return null;
                }

                Vector3d relative = vessel.GetWorldPos3D() - referenceBody.position;
                return ToSnapshot(relative);
            }

            private static Vector3Snapshot CaptureOrbitalVelocity(Vessel vessel)
            {
                if (vessel == null)
                {
                    return null;
                }

                return ToSnapshot(vessel.obt_velocity);
            }

            private static Vector3Snapshot CaptureVesselRootPosition(Vessel vessel, CelestialBody rootBody)
            {
                if (vessel == null || rootBody == null)
                {
                    return null;
                }

                return ToSnapshot(vessel.GetWorldPos3D() - rootBody.position);
            }

            private static Vector3Snapshot CaptureVesselRootVelocity(Vessel vessel, CelestialBody referenceBody, CelestialBody rootBody)
            {
                if (vessel == null || referenceBody == null || rootBody == null)
                {
                    return null;
                }

                Vector3d referenceBodyRootVelocity = referenceBody.GetFrameVel() - rootBody.GetFrameVel();
                return ToSnapshot(vessel.obt_velocity + referenceBodyRootVelocity);
            }

            private static Vector3Snapshot CaptureBodyRootPosition(
                CelestialBody body,
                CelestialBody rootBody,
                double universalTime)
            {
                if (body == null || rootBody == null)
                {
                    return null;
                }

                return ToSnapshot(RootRelativePositionResolver.GetBodyDisplayRootRelative(
                    body,
                    rootBody,
                    universalTime,
                    universalTime));
            }

            private static Vector3Snapshot CaptureBodyTrailSamplePosition(
                CelestialBody body,
                CelestialBody rootBody,
                double sampleUniversalTime,
                double currentUniversalTime)
            {
                if (body == null || rootBody == null || !IsValidUniversalTime(sampleUniversalTime))
                {
                    return null;
                }

                if (body == rootBody)
                {
                    return ToSnapshot(Vector3d.zero);
                }

                try
                {
                    return ToSnapshot(RootRelativePositionResolver.GetBodyRootRelativeForTrailSample(
                        body,
                        rootBody,
                        sampleUniversalTime,
                        currentUniversalTime));
                }
                catch
                {
                    return null;
                }
            }

            private static Vector3Snapshot CaptureBodyRootPositionAtUniversalTime(
                CelestialBody body,
                CelestialBody rootBody,
                double universalTime,
                double currentUniversalTime,
                out string warning)
            {
                warning = null;

                if (body == null || rootBody == null)
                {
                    warning = "Body or root body is unavailable.";
                    return null;
                }

                if (!IsValidUniversalTime(universalTime))
                {
                    warning = "Universal time is not finite.";
                    return null;
                }

                if (body == rootBody)
                {
                    return ToSnapshot(Vector3d.zero);
                }

                try
                {
                    return ToSnapshot(RootRelativePositionResolver.GetBodyRootRelative(
                        body,
                        rootBody,
                        universalTime,
                        currentUniversalTime));
                }
                catch (Exception ex)
                {
                    warning = "Body propagation failed at UT: " + ex.Message;
                    return null;
                }
            }

            private static Vector3Snapshot CaptureBodyRootVelocityAtUniversalTime(CelestialBody body, CelestialBody rootBody, double universalTime, out string warning)
            {
                warning = null;

                if (body == null || rootBody == null || !IsValidUniversalTime(universalTime))
                {
                    warning = "Body, root body, or universal time is unavailable.";
                    return null;
                }

                if (body == rootBody)
                {
                    return ToSnapshot(Vector3d.zero);
                }

                if (body.orbit == null)
                {
                    return CaptureBodyRootVelocity(body, rootBody);
                }

                try
                {
                    Vector3d bodyVelocity = FlipOrbitVector(body.orbit.GetFrameVelAtUT(universalTime));
                    Vector3d rootVelocity = rootBody.orbit != null
                        ? FlipOrbitVector(rootBody.orbit.GetFrameVelAtUT(universalTime))
                        : rootBody.GetFrameVel();
                    return ToSnapshot(bodyVelocity - rootVelocity);
                }
                catch (Exception ex)
                {
                    warning = "Body velocity propagation failed at UT: " + ex.Message;
                    return null;
                }
            }

            private static PlacementSampleSnapshot CapturePlacementSample(
                CelestialBody body,
                CelestialBody rootBody,
                double sampleUniversalTime,
                string sampleRole,
                double patchStartUniversalTime,
                double patchEndUniversalTime,
                double currentUniversalTime)
            {
                if (body == null || rootBody == null)
                {
                    return null;
                }

                if (!IsValidUniversalTime(sampleUniversalTime))
                {
                    return new PlacementSampleSnapshot
                    {
                        SampleRole = sampleRole,
                        TargetBody = body.bodyName,
                        SampleUniversalTimeSeconds = sampleUniversalTime,
                        PositionRootRelativeMeters = null,
                        SampleSource = "celestialBodyOrbitPropagation",
                        SampleWarning = "Sample universal time is not finite."
                    };
                }

                if (!IsUniversalTimeWithinPatchBounds(sampleUniversalTime, patchStartUniversalTime, patchEndUniversalTime))
                {
                    return new PlacementSampleSnapshot
                    {
                        SampleRole = sampleRole,
                        TargetBody = body.bodyName,
                        SampleUniversalTimeSeconds = sampleUniversalTime,
                        PositionRootRelativeMeters = null,
                        SampleSource = "celestialBodyOrbitPropagation",
                        SampleWarning = "Sample universal time is outside patch bounds."
                    };
                }

                Vector3Snapshot position = CaptureBodyTrailSamplePosition(
                    body,
                    rootBody,
                    sampleUniversalTime,
                    currentUniversalTime);

                return new PlacementSampleSnapshot
                {
                    SampleRole = sampleRole,
                    TargetBody = body.bodyName,
                    SampleUniversalTimeSeconds = sampleUniversalTime,
                    PositionRootRelativeMeters = position,
                    SampleSource = "celestialBodyOrbitPropagation",
                    SampleWarning = position == null ? "Body trail sample unavailable." : null
                };
            }

            private static EphemerisSampleSnapshot[] CaptureEphemerisSamples(OrbitPatchSnapshot[] patches, CelestialBody rootBody, double universalTime)
            {
                List<EphemerisSampleSnapshot> samples = new List<EphemerisSampleSnapshot>();
                HashSet<string> dedupeKeys = new HashSet<string>();

                if (patches != null)
                {
                    foreach (OrbitPatchSnapshot patch in patches)
                    {
                        if (patch == null || patch.PlacementSamples == null)
                        {
                            continue;
                        }

                        foreach (PlacementSampleSnapshot placement in patch.PlacementSamples)
                        {
                            AddEphemerisSample(samples, dedupeKeys, placement.TargetBody, placement.SampleRole, placement.SampleUniversalTimeSeconds, placement.PositionRootRelativeMeters, null, placement.SampleSource, placement.SampleWarning);
                        }

                        AddEphemerisSample(samples, dedupeKeys, patch.ReferenceBody, "chainBody", universalTime, CaptureBodyRootPosition(FindBodyByName(patch.ReferenceBody), rootBody, universalTime), CaptureBodyRootVelocity(FindBodyByName(patch.ReferenceBody), rootBody), "celestialBodyCurrentState", null);
                        AddEphemerisSample(samples, dedupeKeys, patch.EncounterBody, "chainBody", universalTime, CaptureBodyRootPosition(FindBodyByName(patch.EncounterBody), rootBody, universalTime), CaptureBodyRootVelocity(FindBodyByName(patch.EncounterBody), rootBody), "celestialBodyCurrentState", null);
                        AddEphemerisSample(samples, dedupeKeys, patch.NextPatchReferenceBody, "chainBody", universalTime, CaptureBodyRootPosition(FindBodyByName(patch.NextPatchReferenceBody), rootBody, universalTime), CaptureBodyRootVelocity(FindBodyByName(patch.NextPatchReferenceBody), rootBody), "celestialBodyCurrentState", null);
                        AddEphemerisSample(samples, dedupeKeys, patch.PreviousPatchReferenceBody, "chainBody", universalTime, CaptureBodyRootPosition(FindBodyByName(patch.PreviousPatchReferenceBody), rootBody, universalTime), CaptureBodyRootVelocity(FindBodyByName(patch.PreviousPatchReferenceBody), rootBody), "celestialBodyCurrentState", null);
                    }
                }

                return samples.ToArray();
            }

            private static void AddEphemerisSample(
                List<EphemerisSampleSnapshot> samples,
                HashSet<string> dedupeKeys,
                string targetBody,
                string sampleRole,
                double sampleUniversalTime,
                Vector3Snapshot position,
                Vector3Snapshot velocity,
                string sampleSource,
                string sampleWarning)
            {
                if (string.IsNullOrEmpty(targetBody) || position == null)
                {
                    return;
                }

                string key = targetBody + "|" + sampleRole + "|" + sampleUniversalTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                if (!dedupeKeys.Add(key))
                {
                    return;
                }

                samples.Add(new EphemerisSampleSnapshot
                {
                    TargetBody = targetBody,
                    SampleRole = sampleRole,
                    SampleUniversalTimeSeconds = sampleUniversalTime,
                    PositionRootRelativeMeters = position,
                    VelocityRootRelativeMetersPerSecond = velocity,
                    ReferenceFrame = RootFrameName,
                    SampleSource = sampleSource,
                    SampleWarning = sampleWarning
                });
            }

            private static string DetermineEphemerisCaptureStatus(OrbitPatchSnapshot[] patches, EphemerisSampleSnapshot[] ephemerisSamples)
            {
                if (patches == null || patches.Length == 0)
                {
                    return ephemerisSamples != null && ephemerisSamples.Length > 0 ? "partial" : "unsupported";
                }

                bool anySampled = false;
                bool anyFailed = false;

                foreach (OrbitPatchSnapshot patch in patches)
                {
                    if (patch == null)
                    {
                        continue;
                    }

                    if (patch.PatchPlacementMode == "multiSampleEphemeris" || patch.PatchPlacementMode == "multiSampleEphemerisPartial")
                    {
                        anySampled = true;
                    }
                    else if (patch.PatchPlacementMode == "currentReferenceBodyPosition")
                    {
                        anyFailed = true;
                    }
                }

                if (anySampled && anyFailed)
                {
                    return "partial";
                }

                if (anySampled)
                {
                    return "ok";
                }

                return anyFailed ? "partial" : "unsupported";
            }

            private static double ValidateEphemerisPropagation(
                CelestialBody rootBody,
                double universalTime,
                CelestialBodySnapshot[] bodies,
                BodyOrbitPathSnapshot[] bodyOrbitPaths,
                out double iconTrailSample0Residual,
                out double bodyOrbitPropagationResidual,
                out double bodyOrbitFlipPropagationResidual,
                out double bodyOrbitSampleResidual,
                out double bodyOrbitAnalyticResidual,
                out double bodyOrbitPeriodClosureResidual,
                out double ephemerisLivePropagationResidual,
                out PositionValidationSnapshot positionValidation)
            {
                iconTrailSample0Residual = 0d;
                bodyOrbitPropagationResidual = 0d;
                bodyOrbitFlipPropagationResidual = 0d;
                bodyOrbitSampleResidual = 0d;
                bodyOrbitAnalyticResidual = 0d;
                bodyOrbitPeriodClosureResidual = 0d;
                ephemerisLivePropagationResidual = 0d;
                positionValidation = new PositionValidationSnapshot
                {
                    WorstBodyName = null,
                    WorstCheck = null,
                    WorstResidualMeters = 0d
                };

                if (rootBody == null || FlightGlobals.Bodies == null || !IsValidUniversalTime(universalTime))
                {
                    return double.NaN;
                }

                double primaryMaxResidual = 0d;

                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body == null || body.orbit == null)
                    {
                        continue;
                    }

                    Vector3Snapshot current = CaptureBodyRootPosition(body, rootBody, universalTime);
                    string warning;
                    Vector3Snapshot propagated = CaptureBodyRootPositionAtUniversalTime(
                        body,
                        rootBody,
                        universalTime + 60d,
                        universalTime,
                        out warning);

                    if (current == null || propagated == null || !string.IsNullOrEmpty(warning))
                    {
                        continue;
                    }

                    double orbitalSeparation = ResidualMeters(current, propagated);
                    ephemerisLivePropagationResidual = Math.Max(ephemerisLivePropagationResidual, orbitalSeparation);
                }

                if (bodyOrbitPaths != null)
                {
                    foreach (BodyOrbitPathSnapshot path in bodyOrbitPaths)
                    {
                        if (path == null || string.IsNullOrEmpty(path.BodyName))
                        {
                            continue;
                        }

                        if (path.Validation != null)
                        {
                            if (!double.IsNaN(path.Validation.LiveToSample0Meters))
                            {
                                iconTrailSample0Residual = Math.Max(
                                    iconTrailSample0Residual,
                                    path.Validation.LiveToSample0Meters);
                                primaryMaxResidual = Math.Max(primaryMaxResidual, path.Validation.LiveToSample0Meters);
                                RecordWorstPositionCheck(
                                    positionValidation,
                                    path.BodyName,
                                    "iconTrailSample0",
                                    path.Validation.LiveToSample0Meters);
                            }

                            if (!double.IsNaN(path.Validation.LiveToAnalyticMeters))
                            {
                                bodyOrbitAnalyticResidual = Math.Max(
                                    bodyOrbitAnalyticResidual,
                                    path.Validation.LiveToAnalyticMeters);
                            }

                            if (!double.IsNaN(path.Validation.MaxSampleToRecomputedMeters))
                            {
                                bodyOrbitSampleResidual = Math.Max(
                                    bodyOrbitSampleResidual,
                                    path.Validation.MaxSampleToRecomputedMeters);
                                primaryMaxResidual = Math.Max(
                                    primaryMaxResidual,
                                    path.Validation.MaxSampleToRecomputedMeters);
                                RecordWorstPositionCheck(
                                    positionValidation,
                                    path.BodyName,
                                    "trailSampleRecomputed",
                                    path.Validation.MaxSampleToRecomputedMeters);
                            }

                            if (!double.IsNaN(path.Validation.PeriodClosureMeters))
                            {
                                bodyOrbitPeriodClosureResidual = Math.Max(
                                    bodyOrbitPeriodClosureResidual,
                                    path.Validation.PeriodClosureMeters);
                            }
                        }

                        if (path.Samples == null || path.Samples.Length == 0)
                        {
                            continue;
                        }

                        CelestialBody body = FindBodyByName(path.BodyName);

                        if (body == null)
                        {
                            continue;
                        }

                        CelestialBodySnapshot bodySnapshot = FindBodySnapshotByName(bodies, path.BodyName);
                        VesselRootPathSampleSnapshot trailSample0 = path.Samples != null && path.Samples.Length > 0
                            ? path.Samples[0]
                            : null;
                        if (bodySnapshot != null
                            && bodySnapshot.PositionRootRelativeMeters != null
                            && trailSample0 != null
                            && trailSample0.PositionRootRelativeMeters != null)
                        {
                            double iconTrail0 = ResidualMeters(
                                bodySnapshot.PositionRootRelativeMeters,
                                trailSample0.PositionRootRelativeMeters);
                            iconTrailSample0Residual = Math.Max(iconTrailSample0Residual, iconTrail0);
                            primaryMaxResidual = Math.Max(primaryMaxResidual, iconTrail0);
                            RecordWorstPositionCheck(
                                positionValidation,
                                path.BodyName,
                                "iconTrailSample0",
                                iconTrail0);
                        }

                        foreach (VesselRootPathSampleSnapshot sample in path.Samples)
                        {
                            if (sample == null || sample.PositionRootRelativeMeters == null)
                            {
                                continue;
                            }

                            string warning;
                            Vector3Snapshot flipRecomputed = CaptureBodyRootPositionAtUniversalTime(
                                body,
                                rootBody,
                                sample.SampleUniversalTimeSeconds,
                                universalTime,
                                out warning);

                            if (flipRecomputed == null || !string.IsNullOrEmpty(warning))
                            {
                                continue;
                            }

                            double flipResidual = ResidualMeters(sample.PositionRootRelativeMeters, flipRecomputed);
                            bodyOrbitFlipPropagationResidual = Math.Max(bodyOrbitFlipPropagationResidual, flipResidual);
                            RecordWorstPositionCheck(
                                positionValidation,
                                path.BodyName,
                                "trailFlipPropagate",
                                flipResidual);
                        }
                    }
                }

                if (bodies != null)
                {
                    foreach (CelestialBodySnapshot bodySnapshot in bodies)
                    {
                        if (bodySnapshot == null
                            || string.IsNullOrEmpty(bodySnapshot.Name)
                            || double.IsNaN(bodySnapshot.LiveVsTrueDeltaMeters))
                        {
                            continue;
                        }

                        RecordWorstPositionCheck(
                            positionValidation,
                            bodySnapshot.Name,
                            "liveVsTrue",
                            bodySnapshot.LiveVsTrueDeltaMeters);
                    }
                }

                bodyOrbitPropagationResidual = bodyOrbitFlipPropagationResidual;
                return primaryMaxResidual;
            }

            private static void RecordWorstPositionCheck(
                PositionValidationSnapshot positionValidation,
                string bodyName,
                string check,
                double residualMeters)
            {
                if (positionValidation == null
                    || string.IsNullOrEmpty(bodyName)
                    || double.IsNaN(residualMeters)
                    || double.IsInfinity(residualMeters))
                {
                    return;
                }

                if (residualMeters > positionValidation.WorstResidualMeters)
                {
                    positionValidation.WorstBodyName = bodyName;
                    positionValidation.WorstCheck = check;
                    positionValidation.WorstResidualMeters = residualMeters;
                }
            }

            private static CelestialBodySnapshot FindBodySnapshotByName(
                CelestialBodySnapshot[] bodies,
                string bodyName)
            {
                if (bodies == null || string.IsNullOrEmpty(bodyName))
                {
                    return null;
                }

                foreach (CelestialBodySnapshot body in bodies)
                {
                    if (body != null && body.Name == bodyName)
                    {
                        return body;
                    }
                }

                return null;
            }

            private static double ResidualMeters(Vector3Snapshot a, Vector3Snapshot b)
            {
                if (a == null || b == null)
                {
                    return 0d;
                }

                Vector3d delta = new Vector3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
                return delta.magnitude;
            }

            private static void EnsureVesselPathSampleAtUniversalTime(
                List<VesselRootPathSampleSnapshot> samples,
                Vessel vessel,
                CelestialBody rootBody,
                double universalTime)
            {
                if (samples == null || vessel == null || rootBody == null || !IsValidUniversalTime(universalTime))
                {
                    return;
                }

                Vector3Snapshot live = ToSnapshot(vessel.GetWorldPos3D() - rootBody.position);
                int existingIndex = -1;

                for (int i = 0; i < samples.Count; i++)
                {
                    VesselRootPathSampleSnapshot sample = samples[i];

                    if (sample == null || sample.PositionRootRelativeMeters == null)
                    {
                        continue;
                    }

                    if (Math.Abs(sample.SampleUniversalTimeSeconds - universalTime)
                        <= VesselPathUniversalTimeToleranceSeconds)
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    samples[existingIndex].PositionRootRelativeMeters = live;
                    samples[existingIndex].SampleUniversalTimeSeconds = universalTime;
                    return;
                }

                int nearestIndex = -1;
                double nearestDistance = double.MaxValue;

                for (int i = 0; i < samples.Count; i++)
                {
                    VesselRootPathSampleSnapshot sample = samples[i];

                    if (sample == null || sample.PositionRootRelativeMeters == null)
                    {
                        continue;
                    }

                    double distance = ResidualMeters(live, sample.PositionRootRelativeMeters);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestIndex = i;
                    }
                }

                VesselRootPathSampleSnapshot liveSample = new VesselRootPathSampleSnapshot
                {
                    SampleUniversalTimeSeconds = universalTime,
                    PositionRootRelativeMeters = live
                };

                if (nearestIndex < 0 || nearestDistance <= VesselPathLiveToleranceMeters)
                {
                    if (nearestIndex >= 0)
                    {
                        samples[nearestIndex] = liveSample;
                    }

                    return;
                }

                int insertIndex = samples.Count;

                for (int i = 0; i < samples.Count; i++)
                {
                    if (samples[i].SampleUniversalTimeSeconds > universalTime)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                samples.Insert(insertIndex, liveSample);
            }

            private static VesselRootPathSampleSnapshot[] CaptureVesselRootPathSamples(
                Vessel vessel,
                Orbit orbit,
                CelestialBody rootBody,
                OrbitPatchSnapshot[] orbitPatches,
                double universalTime)
            {
                List<VesselRootPathSampleSnapshot> samples = new List<VesselRootPathSampleSnapshot>();

                if (vessel == null || orbit == null || rootBody == null || orbitPatches == null)
                {
                    return samples.ToArray();
                }

                OrbitPatchSnapshot activePatch = null;

                foreach (OrbitPatchSnapshot patch in orbitPatches)
                {
                    if (patch != null && patch.IsActivePatch)
                    {
                        activePatch = patch;
                        break;
                    }
                }

                if (activePatch == null)
                {
                    return samples.ToArray();
                }

                RootRelativePositionResolver.EnsureVesselOffsetCalibrated(
                    vessel,
                    orbit,
                    rootBody,
                    universalTime);

                double startUt = activePatch.PatchStartUniversalTimeSeconds;
                double endUt = activePatch.PatchEndUniversalTimeSeconds;

                if (!IsValidUniversalTime(startUt) || !IsValidUniversalTime(endUt))
                {
                    return samples.ToArray();
                }

                double horizonUt = Math.Min(endUt, universalTime + 86400d * 30d);
                double span = Math.Max(horizonUt - startUt, 0d);

                if (span <= 0d)
                {
                    horizonUt = startUt;
                }

                for (int i = 0; i < VesselRootPathSampleCount; i++)
                {
                    double fraction = VesselRootPathSampleCount == 1 ? 0d : i / (double)(VesselRootPathSampleCount - 1);
                    double sampleUt = startUt + span * fraction;

                    try
                    {
                        Vector3d vesselRootRelative = RootRelativePositionResolver.GetVesselRootRelativeForTrailSample(
                            orbit,
                            rootBody,
                            sampleUt,
                            universalTime,
                            vessel);
                        samples.Add(new VesselRootPathSampleSnapshot
                        {
                            SampleUniversalTimeSeconds = sampleUt,
                            PositionRootRelativeMeters = ToSnapshot(vesselRootRelative)
                        });
                    }
                    catch
                    {
                        // Skip invalid vessel propagation samples.
                    }
                }

                EnsureVesselPathSampleAtUniversalTime(samples, vessel, rootBody, universalTime);

                return samples.ToArray();
            }

            private static BodyOrbitPathSnapshot[] CaptureBodyOrbitPaths(CelestialBody rootBody, double universalTime)
            {
                List<BodyOrbitPathSnapshot> paths = new List<BodyOrbitPathSnapshot>();

                if (rootBody == null)
                {
                    return paths.ToArray();
                }

                int captured = 0;
                List<CelestialBody> captureOrder = BuildBodyOrbitPathCaptureOrder(rootBody);

                foreach (CelestialBody body in captureOrder)
                {
                    if (body == null || body.orbit == null || body == rootBody)
                    {
                        continue;
                    }

                    if (captured >= MaxBodyOrbitPathCount)
                    {
                        break;
                    }

                    double period = body.orbit.period;

                    if (double.IsNaN(period) || double.IsInfinity(period) || period <= 0d)
                    {
                        continue;
                    }

                    List<VesselRootPathSampleSnapshot> samples = new List<VesselRootPathSampleSnapshot>();
                    double startUt = universalTime;
                    double endUt = universalTime + period;
                    string warning = null;

                    for (int i = 0; i < BodyOrbitPathSampleCount; i++)
                    {
                        // Never sample at fraction=1.0 (UT+period); KSP orbit APIs misbehave at exact period wrap.
                        double fraction = i / (double)BodyOrbitPathSampleCount;
                        double sampleUt = startUt + (endUt - startUt) * fraction;

                        try
                        {
                            Vector3Snapshot position = CaptureBodyTrailSamplePosition(
                                body,
                                rootBody,
                                sampleUt,
                                universalTime);

                            if (position == null)
                            {
                                warning = "Body orbit sample unavailable.";
                                continue;
                            }

                            Vector3Snapshot parentPosition = null;
                            CelestialBody parent = body.orbit.referenceBody;

                            if (parent != null && parent != body && parent != rootBody)
                            {
                                parentPosition = CaptureBodyTrailSamplePosition(
                                    parent,
                                    rootBody,
                                    sampleUt,
                                    universalTime);
                            }
                            else if (parent == rootBody)
                            {
                                parentPosition = ToSnapshot(Vector3d.zero);
                            }

                            samples.Add(new VesselRootPathSampleSnapshot
                            {
                                SampleUniversalTimeSeconds = sampleUt,
                                PositionRootRelativeMeters = position,
                                ParentPositionRootRelativeMeters = parentPosition
                            });
                        }
                        catch
                        {
                            warning = "Body orbit propagation failed.";
                        }
                    }

                    if (samples.Count < 2)
                    {
                        continue;
                    }

                    CelestialBody referenceBody = body.orbit.referenceBody;
                    BodyOrbitPathSnapshot path = new BodyOrbitPathSnapshot
                    {
                        BodyName = body.bodyName,
                        ReferenceBody = referenceBody != null ? referenceBody.bodyName : null,
                        Classification = ClassifyPatchOrbit(body.orbit),
                        CaptureWarning = warning,
                        Samples = samples.ToArray(),
                        OrbitElements = CaptureBodyOrbitElements(body.orbit, referenceBody, universalTime)
                    };
                    path.Validation = BodyOrbitDiagnostics.ValidatePath(body, rootBody, path, universalTime);
                    paths.Add(path);
                    captured++;
                }

                return paths.ToArray();
            }

            private static List<CelestialBody> BuildBodyOrbitPathCaptureOrder(CelestialBody rootBody)
            {
                List<CelestialBody> moons = new List<CelestialBody>();
                List<CelestialBody> planets = new List<CelestialBody>();
                List<CelestialBody> other = new List<CelestialBody>();

                if (FlightGlobals.Bodies == null)
                {
                    return other;
                }

                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body == null || body.orbit == null || body == rootBody)
                    {
                        continue;
                    }

                    CelestialBody parent = body.orbit.referenceBody;

                    if (parent != null && parent != rootBody && parent != body)
                    {
                        moons.Add(body);
                    }
                    else if (parent == rootBody)
                    {
                        planets.Add(body);
                    }
                    else
                    {
                        other.Add(body);
                    }
                }

                List<CelestialBody> ordered = new List<CelestialBody>();
                ordered.AddRange(moons);
                ordered.AddRange(planets);
                ordered.AddRange(other);
                return ordered;
            }

            private static BodyOrbitElementsSnapshot CaptureBodyOrbitElements(
                Orbit orbit,
                CelestialBody referenceBody,
                double universalTime)
            {
                if (orbit == null)
                {
                    return null;
                }

                double referenceBodyRadius = referenceBody != null ? referenceBody.Radius : 0d;

                return new BodyOrbitElementsSnapshot
                {
                    ReferenceBody = referenceBody != null ? referenceBody.bodyName : null,
                    Classification = ClassifyPatchOrbit(orbit),
                    ReferenceBodyRadiusMeters = referenceBodyRadius,
                    SphereOfInfluenceMeters = referenceBody != null ? referenceBody.sphereOfInfluence : double.NaN,
                    SemiMajorAxisMeters = orbit.semiMajorAxis,
                    SemiLatusRectumMeters = CalculateSemiLatusRectum(orbit.semiMajorAxis, orbit.eccentricity),
                    Eccentricity = orbit.eccentricity,
                    InclinationDegrees = orbit.inclination,
                    LongitudeOfAscendingNodeDegrees = orbit.LAN,
                    ArgumentOfPeriapsisDegrees = orbit.argumentOfPeriapsis,
                    EpochUniversalTimeSeconds = orbit.epoch,
                    PeriodSeconds = orbit.period,
                    TrueAnomalyDegreesAtCapture = orbit.trueAnomaly,
                    MeanAnomalyRadiansAtCapture = orbit.meanAnomaly
                };
            }

            private static string DetermineBodyOrbitCaptureStatus(CelestialBody rootBody)
            {
                if (rootBody == null)
                {
                    return "unsupported";
                }

                return "ok";
            }

            private static double CaptureClosestApproachMeters(Orbit orbit, CelestialBody encounterBody)
            {
                if (orbit == null || encounterBody == null || !IsValidEncounterUniversalTime(orbit.closestTgtApprUT))
                {
                    return double.NaN;
                }

                try
                {
                    double encounterUt = orbit.closestTgtApprUT;
                    CelestialBody rootBody = FindRootBody();
                    if (rootBody == null)
                    {
                        return double.NaN;
                    }

                    Vector3d vesselRootRelative = RootRelativePositionResolver.GetVesselRootRelative(
                        orbit,
                        rootBody,
                        encounterUt,
                        encounterUt);
                    Vector3d encounterRootRelative = RootRelativePositionResolver.GetBodyDisplayRootRelative(
                        encounterBody,
                        rootBody,
                        encounterUt,
                        encounterUt);
                    return (vesselRootRelative - encounterRootRelative).magnitude;
                }
                catch
                {
                    return double.NaN;
                }
            }

            private static Vector3d FlipOrbitVector(Vector3d value)
            {
                return new Vector3d(value.x, -value.y, -value.z);
            }

            private static bool IsValidUniversalTime(double universalTime)
            {
                return !double.IsNaN(universalTime) && !double.IsInfinity(universalTime);
            }

            private static bool IsValidEncounterUniversalTime(double universalTime)
            {
                return IsValidUniversalTime(universalTime) && universalTime > 0d;
            }

            private static bool IsUniversalTimeWithinPatchBounds(double sampleUniversalTime, double patchStartUniversalTime, double patchEndUniversalTime)
            {
                if (!IsValidUniversalTime(sampleUniversalTime))
                {
                    return false;
                }

                if (!IsValidUniversalTime(patchStartUniversalTime) || !IsValidUniversalTime(patchEndUniversalTime))
                {
                    return true;
                }

                double lower = Math.Min(patchStartUniversalTime, patchEndUniversalTime) - 1d;
                double upper = Math.Max(patchStartUniversalTime, patchEndUniversalTime) + 1d;
                return sampleUniversalTime >= lower && sampleUniversalTime <= upper;
            }

            private static CelestialBody FindBodyByName(string bodyName)
            {
                if (string.IsNullOrEmpty(bodyName) || FlightGlobals.Bodies == null)
                {
                    return null;
                }

                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body != null && body.bodyName == bodyName)
                    {
                        return body;
                    }
                }

                return null;
            }

            private static Vector3Snapshot CaptureBodyRootVelocity(CelestialBody body, CelestialBody rootBody)
            {
                if (body == null || rootBody == null)
                {
                    return null;
                }

                return ToSnapshot(body.GetFrameVel() - rootBody.GetFrameVel());
            }

            private static Vector3Snapshot ToSnapshot(Vector3d value)
            {
                return new Vector3Snapshot
                {
                    X = value.x,
                    Y = value.y,
                    Z = value.z
                };
            }

            private static CelestialBodySnapshot[] CaptureBodies(CelestialBody rootBody, double universalTime)
            {
                List<CelestialBodySnapshot> snapshots = new List<CelestialBodySnapshot>();

                if (FlightGlobals.Bodies == null)
                {
                    return snapshots.ToArray();
                }

                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body == null)
                    {
                        continue;
                    }

                    Vector3Snapshot displayPosition = CaptureBodyRootPosition(body, rootBody, universalTime);
                    Vector3d livePosition = RootRelativePositionResolver.GetLiveRootRelative(body, rootBody);
                    Vector3d truePosition;
                    bool hasTruePosition = RootRelativePositionResolver.TryGetTrueRootRelative(
                        body,
                        rootBody,
                        universalTime,
                        out truePosition);
                    double liveVsTrueDelta = hasTruePosition
                        ? (livePosition - truePosition).magnitude
                        : double.NaN;
                    double eclipticLongitude = double.NaN;

                    if (displayPosition != null)
                    {
                        eclipticLongitude = Math.Atan2(displayPosition.X, displayPosition.Z)
                            * (180d / Math.PI);
                    }

                    snapshots.Add(new CelestialBodySnapshot
                    {
                        Name = body.bodyName,
                        ParentBody = body.orbit != null && body.orbit.referenceBody != null
                            ? body.orbit.referenceBody.bodyName
                            : (body.referenceBody != null ? body.referenceBody.bodyName : null),
                        PositionReferenceFrame = rootBody != null ? RootFrameName : null,
                        PositionSampleUniversalTimeSeconds = universalTime,
                        PositionRootRelativeMeters = displayPosition,
                        PositionLiveRootRelativeMeters = ToSnapshot(livePosition),
                        PositionTrueRootRelativeMeters = hasTruePosition ? ToSnapshot(truePosition) : null,
                        LiveVsTrueDeltaMeters = liveVsTrueDelta,
                        EclipticLongitudeDegrees = eclipticLongitude,
                        VelocityReferenceFrame = rootBody != null ? RootFrameName : null,
                        VelocityRootRelativeMetersPerSecond = CaptureBodyRootVelocity(body, rootBody),
                        OrbitReferenceBody = body.orbit != null && body.orbit.referenceBody != null ? body.orbit.referenceBody.bodyName : null,
                        RadiusMeters = body.Radius,
                        GravParameter = body.gravParameter,
                        SphereOfInfluenceMeters = body.sphereOfInfluence,
                        HasAtmosphere = body.atmosphere,
                        AtmosphereDepthMeters = body.atmosphereDepth
                    });
                }

                return snapshots.ToArray();
            }

            private static CelestialBody FindRootBody()
            {
                if (FlightGlobals.Bodies == null)
                {
                    return null;
                }

                CelestialBody namedSun = null;
                CelestialBody firstBody = null;

                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body == null)
                    {
                        continue;
                    }

                    if (firstBody == null)
                    {
                        firstBody = body;
                    }

                    if (body.referenceBody == body)
                    {
                        return body;
                    }

                    if (body.bodyName == "Sun")
                    {
                        namedSun = body;
                    }
                }

                return namedSun != null ? namedSun : firstBody;
            }

            private static double GetUniversalTimeSeconds()
            {
                return Planetarium.GetUniversalTime();
            }

            private static string GetSceneName()
            {
                return HighLogic.LoadedScene.ToString();
            }
        }
    }
}
