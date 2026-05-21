namespace KspWebMap
{
    public sealed class BodyOrbitPathSnapshot
    {
        public string BodyName;
        public string ReferenceBody;
        public string Classification;
        public string CaptureWarning;
        public VesselRootPathSampleSnapshot[] Samples;
        public BodyOrbitElementsSnapshot OrbitElements;
        public BodyOrbitPathValidationSnapshot Validation;
    }
}
