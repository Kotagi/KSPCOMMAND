using System;
using System.Globalization;
using System.Text;

namespace KspWebMap
{
    public static class TelemetryJsonWriter
    {
        public static string WriteTelemetry(TelemetrySnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder(12288);
            WriteSnapshotMetadata(builder, snapshot);
            builder.Append(",\"activeVessel\":");
            WriteActiveVessel(builder, snapshot.ActiveVessel);
            builder.Append(",\"orbit\":");
            WriteOrbit(builder, snapshot.Orbit);
            builder.Append(",\"patchChainStatus\":");
            WriteStringOrNull(builder, snapshot.PatchChainStatus);
            builder.Append(",\"orbitPatches\":");
            WriteOrbitPatches(builder, snapshot.OrbitPatches);
            builder.Append(",\"bodies\":");
            WriteBodies(builder, snapshot.Bodies);
            builder.Append(",\"ephemerisCaptureStatus\":");
            WriteStringOrNull(builder, snapshot.EphemerisCaptureStatus);
            builder.Append(',');
            WriteProperty(builder, "ephemerisValidationResidualMeters", snapshot.EphemerisValidationResidualMeters);
            builder.Append(',');
            WriteProperty(builder, "ephemerisLivePropagationResidualMeters", snapshot.EphemerisLivePropagationResidualMeters);
            builder.Append(',');
            WriteProperty(builder, "iconTrailSample0ResidualMeters", snapshot.IconTrailSample0ResidualMeters);
            builder.Append(',');
            WriteProperty(builder, "bodyOrbitPropagationResidualMeters", snapshot.BodyOrbitPropagationResidualMeters);
            builder.Append(',');
            WriteProperty(builder, "bodyOrbitFlipPropagationResidualMeters", snapshot.BodyOrbitFlipPropagationResidualMeters);
            builder.Append(',');
            WriteProperty(builder, "bodyOrbitSampleResidualMeters", snapshot.BodyOrbitSampleResidualMeters);
            builder.Append(',');
            WriteProperty(builder, "bodyOrbitAnalyticResidualMeters", snapshot.BodyOrbitAnalyticResidualMeters);
            builder.Append(',');
            WriteProperty(builder, "bodyOrbitPeriodClosureResidualMeters", snapshot.BodyOrbitPeriodClosureResidualMeters);
            builder.Append(",\"ephemerisSamples\":");
            WriteEphemerisSamples(builder, snapshot.EphemerisSamples);
            builder.Append(",\"bodyOrbitCaptureStatus\":");
            WriteStringOrNull(builder, snapshot.BodyOrbitCaptureStatus);
            builder.Append(",\"frameDiagnostics\":");
            WriteFrameDiagnostics(builder, snapshot.FrameDiagnostics);
            builder.Append(",\"positionValidation\":");
            WritePositionValidation(builder, snapshot.PositionValidation);
            builder.Append(",\"bodyOrbitPaths\":");
            WriteBodyOrbitPaths(builder, snapshot.BodyOrbitPaths);
            builder.Append('}');
            return builder.ToString();
        }

        public static string WriteDiagnostics(TelemetrySnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder(4096);
            builder.Append('{');
            WriteProperty(builder, "schemaVersion", snapshot != null ? snapshot.SchemaVersion : 0);
            builder.Append(',');
            WriteProperty(builder, "snapshotId", snapshot != null ? snapshot.SnapshotId : 0);
            builder.Append(',');
            WriteProperty(builder, "valid", snapshot != null && snapshot.Valid);
            builder.Append(",\"frameDiagnostics\":");
            WriteFrameDiagnostics(builder, snapshot != null ? snapshot.FrameDiagnostics : null);
            builder.Append(",\"positionValidation\":");
            WritePositionValidation(builder, snapshot != null ? snapshot.PositionValidation : null);
            builder.Append(',');
            WriteProperty(builder, "iconTrailSample0ResidualMeters", snapshot != null ? snapshot.IconTrailSample0ResidualMeters : double.NaN);
            builder.Append(',');
            WriteProperty(builder, "ephemerisValidationResidualMeters", snapshot != null ? snapshot.EphemerisValidationResidualMeters : double.NaN);
            builder.Append(',');
            WriteProperty(builder, "ephemerisLivePropagationResidualMeters", snapshot != null ? snapshot.EphemerisLivePropagationResidualMeters : double.NaN);
            builder.Append(",\"bodyOrbitPaths\":");
            WriteBodyOrbitDiagnosticsOnly(builder, snapshot != null ? snapshot.BodyOrbitPaths : null);
            builder.Append('}');
            return builder.ToString();
        }

        public static string WriteActiveVesselEndpoint(TelemetrySnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder(1024);
            WriteSnapshotMetadata(builder, snapshot);
            builder.Append(",\"activeVessel\":");
            WriteActiveVessel(builder, snapshot.ActiveVessel);
            builder.Append('}');
            return builder.ToString();
        }

        public static string WriteOrbitEndpoint(TelemetrySnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder(1024);
            WriteSnapshotMetadata(builder, snapshot);
            builder.Append(",\"orbit\":");
            WriteOrbit(builder, snapshot.Orbit);
            builder.Append('}');
            return builder.ToString();
        }

        public static string WriteBodiesEndpoint(TelemetrySnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder(2048);
            WriteSnapshotMetadata(builder, snapshot);
            builder.Append(",\"bodies\":");
            WriteBodies(builder, snapshot.Bodies);
            builder.Append('}');
            return builder.ToString();
        }

        private static void WriteSnapshotMetadata(StringBuilder builder, TelemetrySnapshot snapshot)
        {
            builder.Append('{');
            WriteProperty(builder, "schemaVersion", snapshot.SchemaVersion);
            builder.Append(',');
            WriteProperty(builder, "snapshotId", snapshot.SnapshotId);
            builder.Append(',');
            WriteProperty(builder, "capturedAtUtc", FormatDate(snapshot.CapturedAtUtc));
            builder.Append(',');
            WriteProperty(builder, "gameUniversalTimeSeconds", snapshot.GameUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "rootFrameName", snapshot.RootFrameName);
            builder.Append(',');
            WriteProperty(builder, "rootBody", snapshot.RootBody);
            builder.Append(',');
            WriteProperty(builder, "rootFrameOriginBody", snapshot.RootFrameOriginBody);
            builder.Append(',');
            WriteProperty(builder, "rootFrameCapturedAtUniversalTimeSeconds", snapshot.RootFrameCapturedAtUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "rootFrameWarning", snapshot.RootFrameWarning);
            builder.Append(',');
            WriteProperty(builder, "scene", snapshot.Scene);
            builder.Append(',');
            WriteProperty(builder, "valid", snapshot.Valid);
            builder.Append(',');
            WriteProperty(builder, "status", snapshot.Status);
        }

        private static void WriteActiveVessel(StringBuilder builder, ActiveVesselSnapshot vessel)
        {
            if (vessel == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "id", vessel.Id);
            builder.Append(',');
            WriteProperty(builder, "name", vessel.Name);
            builder.Append(',');
            WriteProperty(builder, "type", vessel.Type);
            builder.Append(',');
            WriteProperty(builder, "situation", vessel.Situation);
            builder.Append(',');
            WriteProperty(builder, "mainBody", vessel.MainBody);
            builder.Append(',');
            WriteProperty(builder, "altitudeMeters", vessel.AltitudeMeters);
            builder.Append(',');
            WriteProperty(builder, "radarAltitudeMeters", vessel.RadarAltitudeMeters);
            builder.Append(',');
            WriteProperty(builder, "surfaceSpeedMetersPerSecond", vessel.SurfaceSpeedMetersPerSecond);
            builder.Append(',');
            WriteProperty(builder, "orbitalSpeedMetersPerSecond", vessel.OrbitalSpeedMetersPerSecond);
            builder.Append(',');
            WriteProperty(builder, "positionReferenceFrame", vessel.PositionReferenceFrame);
            builder.Append(',');
            builder.Append("\"positionRelativeToReferenceBodyMeters\":");
            WriteVector(builder, vessel.PositionRelativeToReferenceBodyMeters);
            builder.Append(',');
            builder.Append("\"velocityRelativeToReferenceBodyMetersPerSecond\":");
            WriteVector(builder, vessel.VelocityRelativeToReferenceBodyMetersPerSecond);
            builder.Append(',');
            WriteProperty(builder, "rootPositionReferenceFrame", vessel.RootPositionReferenceFrame);
            builder.Append(',');
            builder.Append("\"positionRootRelativeMeters\":");
            WriteVector(builder, vessel.PositionRootRelativeMeters);
            builder.Append(',');
            WriteProperty(builder, "rootVelocityReferenceFrame", vessel.RootVelocityReferenceFrame);
            builder.Append(',');
            builder.Append("\"velocityRootRelativeMetersPerSecond\":");
            WriteVector(builder, vessel.VelocityRootRelativeMetersPerSecond);
            builder.Append(',');
            builder.Append("\"rootPathSamples\":");
            WriteVesselRootPathSamples(builder, vessel.RootPathSamples);
            builder.Append('}');
        }

        private static void WriteOrbit(StringBuilder builder, OrbitSnapshot orbit)
        {
            if (orbit == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "classification", orbit.Classification);
            builder.Append(',');
            WriteProperty(builder, "referenceBody", orbit.ReferenceBody);
            builder.Append(',');
            WriteProperty(builder, "referenceFrame", orbit.ReferenceFrame);
            builder.Append(',');
            WriteProperty(builder, "referenceBodyRadiusMeters", orbit.ReferenceBodyRadiusMeters);
            builder.Append(',');
            WriteProperty(builder, "sphereOfInfluenceMeters", orbit.SphereOfInfluenceMeters);
            builder.Append(',');
            WriteProperty(builder, "apoapsisMeters", orbit.ApoapsisMeters);
            builder.Append(',');
            WriteProperty(builder, "periapsisMeters", orbit.PeriapsisMeters);
            builder.Append(',');
            WriteProperty(builder, "apoapsisRadiusMeters", orbit.ApoapsisRadiusMeters);
            builder.Append(',');
            WriteProperty(builder, "periapsisRadiusMeters", orbit.PeriapsisRadiusMeters);
            builder.Append(',');
            WriteProperty(builder, "semiMajorAxisMeters", orbit.SemiMajorAxisMeters);
            builder.Append(',');
            WriteProperty(builder, "semiLatusRectumMeters", orbit.SemiLatusRectumMeters);
            builder.Append(',');
            WriteProperty(builder, "eccentricity", orbit.Eccentricity);
            builder.Append(',');
            WriteProperty(builder, "inclinationDegrees", orbit.InclinationDegrees);
            builder.Append(',');
            WriteProperty(builder, "longitudeOfAscendingNodeDegrees", orbit.LongitudeOfAscendingNodeDegrees);
            builder.Append(',');
            WriteProperty(builder, "argumentOfPeriapsisDegrees", orbit.ArgumentOfPeriapsisDegrees);
            builder.Append(',');
            WriteProperty(builder, "trueAnomalyDegrees", orbit.TrueAnomalyDegrees);
            builder.Append(',');
            WriteProperty(builder, "meanAnomalyRadians", orbit.MeanAnomalyRadians);
            builder.Append(',');
            WriteProperty(builder, "epochUniversalTimeSeconds", orbit.EpochUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "patchStartUniversalTimeSeconds", orbit.PatchStartUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "patchEndUniversalTimeSeconds", orbit.PatchEndUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "patchStartTransition", orbit.PatchStartTransition);
            builder.Append(',');
            WriteProperty(builder, "patchEndTransition", orbit.PatchEndTransition);
            builder.Append(',');
            WriteProperty(builder, "activePatch", orbit.ActivePatch);
            builder.Append(',');
            WriteProperty(builder, "nextPatchReferenceBody", orbit.NextPatchReferenceBody);
            builder.Append(',');
            WriteProperty(builder, "timeToTransitionSeconds", orbit.TimeToTransitionSeconds);
            builder.Append(',');
            WriteProperty(builder, "periodSeconds", orbit.PeriodSeconds);
            builder.Append(',');
            WriteProperty(builder, "timeToApoapsisSeconds", orbit.TimeToApoapsisSeconds);
            builder.Append(',');
            WriteProperty(builder, "timeToPeriapsisSeconds", orbit.TimeToPeriapsisSeconds);
            builder.Append('}');
        }

        private static void WriteOrbitPatches(StringBuilder builder, OrbitPatchSnapshot[] patches)
        {
            builder.Append('[');

            if (patches != null)
            {
                for (int i = 0; i < patches.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    WriteOrbitPatch(builder, patches[i]);
                }
            }

            builder.Append(']');
        }

        private static void WriteOrbitPatch(StringBuilder builder, OrbitPatchSnapshot patch)
        {
            if (patch == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "patchIndex", patch.PatchIndex);
            builder.Append(',');
            WriteProperty(builder, "isActivePatch", patch.IsActivePatch);
            builder.Append(',');
            WriteProperty(builder, "classification", patch.Classification);
            builder.Append(',');
            WriteProperty(builder, "referenceBody", patch.ReferenceBody);
            builder.Append(',');
            WriteProperty(builder, "referenceFrame", patch.ReferenceFrame);
            builder.Append(',');
            builder.Append("\"referenceBodyPositionRootRelativeMeters\":");
            WriteVector(builder, patch.ReferenceBodyPositionRootRelativeMeters);
            builder.Append(',');
            WriteProperty(builder, "referenceBodyPositionSampleUniversalTimeSeconds", patch.ReferenceBodyPositionSampleUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "patchPlacementMode", patch.PatchPlacementMode);
            builder.Append(',');
            WriteProperty(builder, "patchPlacementWarning", patch.PatchPlacementWarning);
            builder.Append(',');
            builder.Append("\"placementSamples\":");
            WritePlacementSamples(builder, patch.PlacementSamples);
            builder.Append(',');
            WriteProperty(builder, "referenceBodyRadiusMeters", patch.ReferenceBodyRadiusMeters);
            builder.Append(',');
            WriteProperty(builder, "sphereOfInfluenceMeters", patch.SphereOfInfluenceMeters);
            builder.Append(',');
            WriteProperty(builder, "apoapsisMeters", patch.ApoapsisMeters);
            builder.Append(',');
            WriteProperty(builder, "periapsisMeters", patch.PeriapsisMeters);
            builder.Append(',');
            WriteProperty(builder, "apoapsisRadiusMeters", patch.ApoapsisRadiusMeters);
            builder.Append(',');
            WriteProperty(builder, "periapsisRadiusMeters", patch.PeriapsisRadiusMeters);
            builder.Append(',');
            WriteProperty(builder, "semiMajorAxisMeters", patch.SemiMajorAxisMeters);
            builder.Append(',');
            WriteProperty(builder, "semiLatusRectumMeters", patch.SemiLatusRectumMeters);
            builder.Append(',');
            WriteProperty(builder, "eccentricity", patch.Eccentricity);
            builder.Append(',');
            WriteProperty(builder, "inclinationDegrees", patch.InclinationDegrees);
            builder.Append(',');
            WriteProperty(builder, "longitudeOfAscendingNodeDegrees", patch.LongitudeOfAscendingNodeDegrees);
            builder.Append(',');
            WriteProperty(builder, "argumentOfPeriapsisDegrees", patch.ArgumentOfPeriapsisDegrees);
            builder.Append(',');
            WriteProperty(builder, "trueAnomalyDegrees", patch.TrueAnomalyDegrees);
            builder.Append(',');
            WriteProperty(builder, "meanAnomalyRadians", patch.MeanAnomalyRadians);
            builder.Append(',');
            WriteProperty(builder, "epochUniversalTimeSeconds", patch.EpochUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "patchStartUniversalTimeSeconds", patch.PatchStartUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "patchEndUniversalTimeSeconds", patch.PatchEndUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "patchStartTransition", patch.PatchStartTransition);
            builder.Append(',');
            WriteProperty(builder, "patchEndTransition", patch.PatchEndTransition);
            builder.Append(',');
            WriteProperty(builder, "timeToPatchStartSeconds", patch.TimeToPatchStartSeconds);
            builder.Append(',');
            WriteProperty(builder, "timeToPatchEndSeconds", patch.TimeToPatchEndSeconds);
            builder.Append(',');
            WriteProperty(builder, "previousPatchReferenceBody", patch.PreviousPatchReferenceBody);
            builder.Append(',');
            WriteProperty(builder, "nextPatchReferenceBody", patch.NextPatchReferenceBody);
            builder.Append(',');
            WriteProperty(builder, "encounterBody", patch.EncounterBody);
            builder.Append(',');
            WriteProperty(builder, "encounterLevel", patch.EncounterLevel);
            builder.Append(',');
            WriteProperty(builder, "closestEncounterUniversalTimeSeconds", patch.ClosestEncounterUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "closestApproachMeters", patch.ClosestApproachMeters);
            builder.Append(',');
            WriteProperty(builder, "captureWarning", patch.CaptureWarning);
            builder.Append(',');
            WriteProperty(builder, "periodSeconds", patch.PeriodSeconds);
            builder.Append(',');
            WriteProperty(builder, "timeToApoapsisSeconds", patch.TimeToApoapsisSeconds);
            builder.Append(',');
            WriteProperty(builder, "timeToPeriapsisSeconds", patch.TimeToPeriapsisSeconds);
            builder.Append('}');
        }

        private static void WriteBodies(StringBuilder builder, CelestialBodySnapshot[] bodies)
        {
            builder.Append('[');

            if (bodies != null)
            {
                for (int i = 0; i < bodies.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    WriteBody(builder, bodies[i]);
                }
            }

            builder.Append(']');
        }

        private static void WriteBody(StringBuilder builder, CelestialBodySnapshot body)
        {
            if (body == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "name", body.Name);
            builder.Append(',');
            WriteProperty(builder, "parentBody", body.ParentBody);
            builder.Append(',');
            WriteProperty(builder, "positionReferenceFrame", body.PositionReferenceFrame);
            builder.Append(',');
            WriteProperty(builder, "positionSampleUniversalTimeSeconds", body.PositionSampleUniversalTimeSeconds);
            builder.Append(',');
            builder.Append("\"positionRootRelativeMeters\":");
            WriteVector(builder, body.PositionRootRelativeMeters);
            builder.Append(',');
            builder.Append("\"positionLiveRootRelativeMeters\":");
            WriteVector(builder, body.PositionLiveRootRelativeMeters);
            builder.Append(',');
            builder.Append("\"positionTrueRootRelativeMeters\":");
            WriteVector(builder, body.PositionTrueRootRelativeMeters);
            builder.Append(',');
            WriteProperty(builder, "liveVsTrueDeltaMeters", body.LiveVsTrueDeltaMeters);
            builder.Append(',');
            WriteProperty(builder, "eclipticLongitudeDegrees", body.EclipticLongitudeDegrees);
            builder.Append(',');
            WriteProperty(builder, "velocityReferenceFrame", body.VelocityReferenceFrame);
            builder.Append(',');
            builder.Append("\"velocityRootRelativeMetersPerSecond\":");
            WriteVector(builder, body.VelocityRootRelativeMetersPerSecond);
            builder.Append(',');
            WriteProperty(builder, "orbitReferenceBody", body.OrbitReferenceBody);
            builder.Append(',');
            WriteProperty(builder, "radiusMeters", body.RadiusMeters);
            builder.Append(',');
            WriteProperty(builder, "gravParameter", body.GravParameter);
            builder.Append(',');
            WriteProperty(builder, "sphereOfInfluenceMeters", body.SphereOfInfluenceMeters);
            builder.Append(',');
            WriteProperty(builder, "hasAtmosphere", body.HasAtmosphere);
            builder.Append(',');
            WriteProperty(builder, "atmosphereDepthMeters", body.AtmosphereDepthMeters);
            builder.Append(',');
            WriteProperty(builder, "bodyTextureUrl", body.BodyTextureUrl);
            builder.Append(',');
            WriteProperty(builder, "bodyTextureRevision", body.BodyTextureRevision);
            builder.Append(',');
            WriteProperty(builder, "bodyTextureStatus", body.BodyTextureStatus);
            builder.Append('}');
        }

        private static void WriteEphemerisSamples(StringBuilder builder, EphemerisSampleSnapshot[] samples)
        {
            builder.Append('[');

            if (samples != null)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    WriteEphemerisSample(builder, samples[i]);
                }
            }

            builder.Append(']');
        }

        private static void WriteEphemerisSample(StringBuilder builder, EphemerisSampleSnapshot sample)
        {
            if (sample == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "targetBody", sample.TargetBody);
            builder.Append(',');
            WriteProperty(builder, "sampleRole", sample.SampleRole);
            builder.Append(',');
            WriteProperty(builder, "sampleUniversalTimeSeconds", sample.SampleUniversalTimeSeconds);
            builder.Append(',');
            builder.Append("\"positionRootRelativeMeters\":");
            WriteVector(builder, sample.PositionRootRelativeMeters);
            builder.Append(',');
            builder.Append("\"velocityRootRelativeMetersPerSecond\":");
            WriteVector(builder, sample.VelocityRootRelativeMetersPerSecond);
            builder.Append(',');
            WriteProperty(builder, "referenceFrame", sample.ReferenceFrame);
            builder.Append(',');
            WriteProperty(builder, "sampleSource", sample.SampleSource);
            builder.Append(',');
            WriteProperty(builder, "sampleWarning", sample.SampleWarning);
            builder.Append('}');
        }

        private static void WritePlacementSamples(StringBuilder builder, PlacementSampleSnapshot[] samples)
        {
            builder.Append('[');

            if (samples != null)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    WritePlacementSample(builder, samples[i]);
                }
            }

            builder.Append(']');
        }

        private static void WritePlacementSample(StringBuilder builder, PlacementSampleSnapshot sample)
        {
            if (sample == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "sampleRole", sample.SampleRole);
            builder.Append(',');
            WriteProperty(builder, "targetBody", sample.TargetBody);
            builder.Append(',');
            WriteProperty(builder, "sampleUniversalTimeSeconds", sample.SampleUniversalTimeSeconds);
            builder.Append(',');
            builder.Append("\"positionRootRelativeMeters\":");
            WriteVector(builder, sample.PositionRootRelativeMeters);
            builder.Append(',');
            WriteProperty(builder, "sampleSource", sample.SampleSource);
            builder.Append(',');
            WriteProperty(builder, "sampleWarning", sample.SampleWarning);
            builder.Append('}');
        }

        private static void WriteVesselRootPathSamples(StringBuilder builder, VesselRootPathSampleSnapshot[] samples)
        {
            builder.Append('[');

            if (samples != null)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    WriteVesselRootPathSample(builder, samples[i]);
                }
            }

            builder.Append(']');
        }

        private static void WriteVesselRootPathSample(StringBuilder builder, VesselRootPathSampleSnapshot sample)
        {
            if (sample == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "sampleUniversalTimeSeconds", sample.SampleUniversalTimeSeconds);
            builder.Append(',');
            builder.Append("\"positionRootRelativeMeters\":");
            WriteVector(builder, sample.PositionRootRelativeMeters);
            builder.Append(',');
            builder.Append("\"parentPositionRootRelativeMeters\":");
            WriteVector(builder, sample.ParentPositionRootRelativeMeters);
            builder.Append('}');
        }

        private static void WriteVector(StringBuilder builder, Vector3Snapshot vector)
        {
            if (vector == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "x", vector.X);
            builder.Append(',');
            WriteProperty(builder, "y", vector.Y);
            builder.Append(',');
            WriteProperty(builder, "z", vector.Z);
            builder.Append('}');
        }

        private static void WriteProperty(StringBuilder builder, string name, string value)
        {
            WriteString(builder, name);
            builder.Append(':');
            WriteStringOrNull(builder, value);
        }

        private static void WriteStringOrNull(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
            }
            else
            {
                WriteString(builder, value);
            }
        }

        private static void WriteProperty(StringBuilder builder, string name, bool value)
        {
            WriteString(builder, name);
            builder.Append(':');
            builder.Append(value ? "true" : "false");
        }

        private static void WriteProperty(StringBuilder builder, string name, int value)
        {
            WriteString(builder, name);
            builder.Append(':');
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void WriteProperty(StringBuilder builder, string name, long value)
        {
            WriteString(builder, name);
            builder.Append(':');
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void WriteProperty(StringBuilder builder, string name, double value)
        {
            WriteString(builder, name);
            builder.Append(':');

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                builder.Append("null");
            }
            else
            {
                builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
        }

        private static void WriteBodyOrbitPaths(StringBuilder builder, BodyOrbitPathSnapshot[] paths)
        {
            builder.Append('[');

            if (paths != null)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    WriteBodyOrbitPath(builder, paths[i]);
                }
            }

            builder.Append(']');
        }

        private static void WriteBodyOrbitPath(StringBuilder builder, BodyOrbitPathSnapshot path)
        {
            if (path == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "bodyName", path.BodyName);
            builder.Append(',');
            WriteProperty(builder, "referenceBody", path.ReferenceBody);
            builder.Append(',');
            WriteProperty(builder, "classification", path.Classification);
            builder.Append(',');
            WriteProperty(builder, "captureWarning", path.CaptureWarning);
            builder.Append(",\"orbitElements\":");
            WriteBodyOrbitElements(builder, path.OrbitElements);
            builder.Append(",\"samples\":");
            WriteVesselRootPathSamples(builder, path.Samples);
            builder.Append(",\"validation\":");
            WriteBodyOrbitPathValidation(builder, path.Validation);
            builder.Append('}');
        }

        private static void WriteBodyOrbitElements(StringBuilder builder, BodyOrbitElementsSnapshot elements)
        {
            if (elements == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "referenceBody", elements.ReferenceBody);
            builder.Append(',');
            WriteProperty(builder, "classification", elements.Classification);
            builder.Append(',');
            WriteProperty(builder, "referenceBodyRadiusMeters", elements.ReferenceBodyRadiusMeters);
            builder.Append(',');
            WriteProperty(builder, "sphereOfInfluenceMeters", elements.SphereOfInfluenceMeters);
            builder.Append(',');
            WriteProperty(builder, "semiMajorAxisMeters", elements.SemiMajorAxisMeters);
            builder.Append(',');
            WriteProperty(builder, "semiLatusRectumMeters", elements.SemiLatusRectumMeters);
            builder.Append(',');
            WriteProperty(builder, "eccentricity", elements.Eccentricity);
            builder.Append(',');
            WriteProperty(builder, "inclinationDegrees", elements.InclinationDegrees);
            builder.Append(',');
            WriteProperty(builder, "longitudeOfAscendingNodeDegrees", elements.LongitudeOfAscendingNodeDegrees);
            builder.Append(',');
            WriteProperty(builder, "argumentOfPeriapsisDegrees", elements.ArgumentOfPeriapsisDegrees);
            builder.Append(',');
            WriteProperty(builder, "epochUniversalTimeSeconds", elements.EpochUniversalTimeSeconds);
            builder.Append(',');
            WriteProperty(builder, "periodSeconds", elements.PeriodSeconds);
            builder.Append(',');
            WriteProperty(builder, "trueAnomalyDegreesAtCapture", elements.TrueAnomalyDegreesAtCapture);
            builder.Append(',');
            WriteProperty(builder, "meanAnomalyRadiansAtCapture", elements.MeanAnomalyRadiansAtCapture);
            builder.Append('}');
        }

        private static void WritePositionValidation(StringBuilder builder, PositionValidationSnapshot validation)
        {
            if (validation == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "worstBodyName", validation.WorstBodyName);
            builder.Append(',');
            WriteProperty(builder, "worstCheck", validation.WorstCheck);
            builder.Append(',');
            WriteProperty(builder, "worstResidualMeters", validation.WorstResidualMeters);
            builder.Append('}');
        }

        private static void WriteFrameDiagnostics(StringBuilder builder, FrameDiagnosticsSnapshot diagnostics)
        {
            if (diagnostics == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "resolverVersion", diagnostics.ResolverVersion);
            builder.Append(',');
            WriteProperty(builder, "orbitOffsetMode", diagnostics.OrbitOffsetMode);
            builder.Append(',');
            WriteProperty(builder, "vesselOffsetMode", diagnostics.VesselOffsetMode);
            builder.Append(',');
            WriteProperty(builder, "pluginBuildUtc", diagnostics.PluginBuildUtc);
            builder.Append(',');
            WriteProperty(builder, "captureDurationMs", diagnostics.CaptureDurationMs);
            builder.Append(',');
            WriteProperty(builder, "bodiesCaptured", diagnostics.BodiesCaptured);
            builder.Append(',');
            WriteProperty(builder, "bodyPathsCaptured", diagnostics.BodyPathsCaptured);
            builder.Append('}');
        }

        private static void WriteBodyOrbitDiagnosticsOnly(StringBuilder builder, BodyOrbitPathSnapshot[] paths)
        {
            builder.Append('[');

            if (paths != null)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    BodyOrbitPathSnapshot path = paths[i];
                    builder.Append('{');
                    WriteProperty(builder, "bodyName", path != null ? path.BodyName : null);
                    builder.Append(",\"validation\":");
                    WriteBodyOrbitPathValidation(builder, path != null ? path.Validation : null);
                    builder.Append('}');
                }
            }

            builder.Append(']');
        }

        private static void WriteBodyOrbitPathValidation(StringBuilder builder, BodyOrbitPathValidationSnapshot validation)
        {
            if (validation == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('{');
            WriteProperty(builder, "liveToSample0Meters", validation.LiveToSample0Meters);
            builder.Append(',');
            WriteProperty(builder, "liveToAnalyticMeters", validation.LiveToAnalyticMeters);
            builder.Append(',');
            WriteProperty(builder, "maxSampleToRecomputedMeters", validation.MaxSampleToRecomputedMeters);
            builder.Append(',');
            WriteProperty(builder, "maxSampleToTrailFrameMeters", validation.MaxSampleToTrailFrameMeters);
            builder.Append(',');
            WriteProperty(builder, "periodClosureMeters", validation.PeriodClosureMeters);
            builder.Append(',');
            builder.Append("\"planeNormalRootRelative\":");
            WriteVector(builder, validation.PlaneNormalRootRelative);
            builder.Append(',');
            WriteProperty(builder, "planeAngleToAnalyticDegrees", validation.PlaneAngleToAnalyticDegrees);
            builder.Append(',');
            WriteProperty(builder, "trailRenderMode", validation.TrailRenderMode);
            builder.Append(',');
            WriteProperty(builder, "trailWarning", validation.TrailWarning);
            builder.Append('}');
        }

        private static string FormatDate(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }
    }
}
