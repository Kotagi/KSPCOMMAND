namespace KspWebMap
{
    public sealed class CelestialBodySnapshot
    {
        public string Name;
        public string ParentBody;
        public string PositionReferenceFrame;
        public double PositionSampleUniversalTimeSeconds;
        public Vector3Snapshot PositionRootRelativeMeters;
        public Vector3Snapshot PositionLiveRootRelativeMeters;
        public Vector3Snapshot PositionTrueRootRelativeMeters;
        public double LiveVsTrueDeltaMeters;
        public double EclipticLongitudeDegrees;
        public string VelocityReferenceFrame;
        public Vector3Snapshot VelocityRootRelativeMetersPerSecond;
        public string OrbitReferenceBody;
        public double RadiusMeters;
        public double GravParameter;
        public double SphereOfInfluenceMeters;
        public bool HasAtmosphere;
        public double AtmosphereDepthMeters;
    }
}
