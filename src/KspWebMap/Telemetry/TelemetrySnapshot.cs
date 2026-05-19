using System;

namespace KspWebMap
{
    public sealed class TelemetrySnapshot
    {
        public const int CurrentSchemaVersion = 7;

        public int SchemaVersion;
        public long SnapshotId;
        public DateTime CapturedAtUtc;
        public double GameUniversalTimeSeconds;
        public string RootFrameName;
        public string RootBody;
        public string RootFrameOriginBody;
        public double RootFrameCapturedAtUniversalTimeSeconds;
        public string RootFrameWarning;
        public string Scene;
        public bool Valid;
        public string Status;
        public ActiveVesselSnapshot ActiveVessel;
        public OrbitSnapshot Orbit;
        public OrbitPatchSnapshot[] OrbitPatches;
        public string PatchChainStatus;
        public CelestialBodySnapshot[] Bodies;
        public EphemerisSampleSnapshot[] EphemerisSamples;
        public string EphemerisCaptureStatus;
        public double EphemerisValidationResidualMeters;
        public BodyOrbitPathSnapshot[] BodyOrbitPaths;
        public string BodyOrbitCaptureStatus;

        public static TelemetrySnapshot CreateInvalid(long snapshotId, string status)
        {
            return new TelemetrySnapshot
            {
                SchemaVersion = CurrentSchemaVersion,
                SnapshotId = snapshotId,
                CapturedAtUtc = DateTime.UtcNow,
                GameUniversalTimeSeconds = 0d,
                RootFrameName = "solarSystemRootCenteredInertial",
                RootBody = null,
                RootFrameOriginBody = null,
                RootFrameCapturedAtUniversalTimeSeconds = 0d,
                RootFrameWarning = "Root frame unavailable for invalid telemetry snapshot.",
                Scene = "Unknown",
                Valid = false,
                Status = status,
                ActiveVessel = null,
                Orbit = null,
                OrbitPatches = new OrbitPatchSnapshot[0],
                PatchChainStatus = "unsupported",
                Bodies = new CelestialBodySnapshot[0],
                EphemerisSamples = new EphemerisSampleSnapshot[0],
                EphemerisCaptureStatus = "unsupported",
                EphemerisValidationResidualMeters = double.NaN,
                BodyOrbitPaths = new BodyOrbitPathSnapshot[0],
                BodyOrbitCaptureStatus = "unsupported"
            };
        }
    }
}
