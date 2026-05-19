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
            private const double EphemerisValidationToleranceSeconds = 1d;
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
                double universalTime = GetUniversalTimeSeconds();
                CelestialBody rootBody = FindRootBody();
                string rootFrameWarning = rootBody == null ? "Root body could not be identified." : null;
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
                        EphemerisValidationResidualMeters = double.NaN
                    };
                }

                Orbit orbit = vessel.orbit;
                string patchChainStatus;
                OrbitPatchSnapshot[] orbitPatches = CaptureOrbitPatches(vessel, orbit, universalTime, rootBody, out patchChainStatus);
                EphemerisSampleSnapshot[] ephemerisSamples = CaptureEphemerisSamples(orbitPatches, rootBody, universalTime);
                string ephemerisCaptureStatus = DetermineEphemerisCaptureStatus(orbitPatches, ephemerisSamples);
                double ephemerisValidationResidual = ValidateEphemerisPropagation(rootBody, universalTime);

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
                    EphemerisValidationResidualMeters = ephemerisValidationResidual
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
                    : CaptureBodyRootPosition(referenceBody, rootBody);

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
                    IsActivePatch = orbit.activePatch,
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

            private static Vector3Snapshot CaptureBodyRootPosition(CelestialBody body, CelestialBody rootBody)
            {
                if (body == null || rootBody == null)
                {
                    return null;
                }

                return ToSnapshot(body.position - rootBody.position);
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
                    Vector3d bodyWorld = GetBodyWorldPositionAtUniversalTime(body, universalTime, currentUniversalTime);
                    Vector3d rootWorld = GetBodyWorldPositionAtUniversalTime(rootBody, universalTime, currentUniversalTime);
                    return ToSnapshot(bodyWorld - rootWorld);
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

                string warning;
                Vector3Snapshot position = CaptureBodyRootPositionAtUniversalTime(body, rootBody, sampleUniversalTime, currentUniversalTime, out warning);

                return new PlacementSampleSnapshot
                {
                    SampleRole = sampleRole,
                    TargetBody = body.bodyName,
                    SampleUniversalTimeSeconds = sampleUniversalTime,
                    PositionRootRelativeMeters = position,
                    SampleSource = "celestialBodyOrbitPropagation",
                    SampleWarning = warning
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

                        AddEphemerisSample(samples, dedupeKeys, patch.ReferenceBody, "chainBody", universalTime, CaptureBodyRootPosition(FindBodyByName(patch.ReferenceBody), rootBody), CaptureBodyRootVelocity(FindBodyByName(patch.ReferenceBody), rootBody), "celestialBodyCurrentState", null);
                        AddEphemerisSample(samples, dedupeKeys, patch.EncounterBody, "chainBody", universalTime, CaptureBodyRootPosition(FindBodyByName(patch.EncounterBody), rootBody), CaptureBodyRootVelocity(FindBodyByName(patch.EncounterBody), rootBody), "celestialBodyCurrentState", null);
                        AddEphemerisSample(samples, dedupeKeys, patch.NextPatchReferenceBody, "chainBody", universalTime, CaptureBodyRootPosition(FindBodyByName(patch.NextPatchReferenceBody), rootBody), CaptureBodyRootVelocity(FindBodyByName(patch.NextPatchReferenceBody), rootBody), "celestialBodyCurrentState", null);
                        AddEphemerisSample(samples, dedupeKeys, patch.PreviousPatchReferenceBody, "chainBody", universalTime, CaptureBodyRootPosition(FindBodyByName(patch.PreviousPatchReferenceBody), rootBody), CaptureBodyRootVelocity(FindBodyByName(patch.PreviousPatchReferenceBody), rootBody), "celestialBodyCurrentState", null);
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

            private static double ValidateEphemerisPropagation(CelestialBody rootBody, double universalTime)
            {
                if (rootBody == null || FlightGlobals.Bodies == null || !IsValidUniversalTime(universalTime))
                {
                    return double.NaN;
                }

                double maxResidual = 0d;

                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body == null || body.orbit == null)
                    {
                        continue;
                    }

                    Vector3Snapshot current = CaptureBodyRootPosition(body, rootBody);
                    string warning;
                    Vector3Snapshot propagated = CaptureBodyRootPositionAtUniversalTime(body, rootBody, universalTime, universalTime, out warning);

                    if (current == null || propagated == null || !string.IsNullOrEmpty(warning))
                    {
                        continue;
                    }

                    Vector3d delta = new Vector3d(
                        current.X - propagated.X,
                        current.Y - propagated.Y,
                        current.Z - propagated.Z);
                    maxResidual = Math.Max(maxResidual, delta.magnitude);
                }

                return maxResidual;
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
                        Vector3d vesselWorld = FlipOrbitVector(orbit.getTruePositionAtUT(sampleUt));
                        Vector3d rootWorld = GetBodyWorldPositionAtUniversalTime(rootBody, sampleUt, universalTime);
                        samples.Add(new VesselRootPathSampleSnapshot
                        {
                            SampleUniversalTimeSeconds = sampleUt,
                            PositionRootRelativeMeters = ToSnapshot(vesselWorld - rootWorld)
                        });
                    }
                    catch
                    {
                        // Skip invalid vessel propagation samples.
                    }
                }

                return samples.ToArray();
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
                    Vector3d vesselWorld = FlipOrbitVector(orbit.getTruePositionAtUT(encounterUt));
                    Vector3d encounterWorld = GetBodyWorldPositionAtUniversalTime(encounterBody, encounterUt, encounterUt);
                    return (vesselWorld - encounterWorld).magnitude;
                }
                catch
                {
                    return double.NaN;
                }
            }

            private static Vector3d GetBodyWorldPositionAtUniversalTime(CelestialBody body, double universalTime, double currentUniversalTime)
            {
                if (body == null)
                {
                    return Vector3d.zero;
                }

                if (body.orbit == null)
                {
                    return body.position;
                }

                if (IsValidUniversalTime(currentUniversalTime)
                    && Math.Abs(universalTime - currentUniversalTime) <= EphemerisValidationToleranceSeconds)
                {
                    return body.position;
                }

                return FlipOrbitVector(body.orbit.getTruePositionAtUT(universalTime));
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

                    snapshots.Add(new CelestialBodySnapshot
                    {
                        Name = body.bodyName,
                        ParentBody = body.referenceBody != null ? body.referenceBody.bodyName : null,
                        PositionReferenceFrame = rootBody != null ? RootFrameName : null,
                        PositionSampleUniversalTimeSeconds = universalTime,
                        PositionRootRelativeMeters = CaptureBodyRootPosition(body, rootBody),
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
