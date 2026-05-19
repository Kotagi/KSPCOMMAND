namespace KspWebMap
{
    public sealed class ActiveVesselSnapshot
    {
        public string Id;
        public string Name;
        public string Type;
        public string Situation;
        public string MainBody;
        public double AltitudeMeters;
        public double RadarAltitudeMeters;
        public double SurfaceSpeedMetersPerSecond;
        public double OrbitalSpeedMetersPerSecond;
        public string PositionReferenceFrame;
        public Vector3Snapshot PositionRelativeToReferenceBodyMeters;
        public Vector3Snapshot VelocityRelativeToReferenceBodyMetersPerSecond;
        public string RootPositionReferenceFrame;
        public Vector3Snapshot PositionRootRelativeMeters;
        public string RootVelocityReferenceFrame;
        public Vector3Snapshot VelocityRootRelativeMetersPerSecond;
        public VesselRootPathSampleSnapshot[] RootPathSamples;
    }
}
