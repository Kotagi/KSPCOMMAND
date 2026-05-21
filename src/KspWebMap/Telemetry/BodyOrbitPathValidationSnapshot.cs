namespace KspWebMap
{
    public sealed class BodyOrbitPathValidationSnapshot
    {
        public double LiveToSample0Meters;
        public double LiveToAnalyticMeters;
        public double MaxSampleToRecomputedMeters;
        public double MaxSampleToTrailFrameMeters;
        public double PeriodClosureMeters;
        public Vector3Snapshot PlaneNormalRootRelative;
        public double PlaneAngleToAnalyticDegrees;
        public string TrailRenderMode;
        public string TrailWarning;
    }
}
