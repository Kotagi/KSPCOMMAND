namespace KspWebMap
{
    public sealed class EphemerisSampleSnapshot
    {
        public string TargetBody;
        public string SampleRole;
        public double SampleUniversalTimeSeconds;
        public Vector3Snapshot PositionRootRelativeMeters;
        public Vector3Snapshot VelocityRootRelativeMetersPerSecond;
        public string ReferenceFrame;
        public string SampleSource;
        public string SampleWarning;
    }
}
