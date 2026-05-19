namespace KspWebMap
{
    public sealed class OrbitSnapshot
    {
        public string Classification;
        public string ReferenceBody;
        public string ReferenceFrame;
        public double ReferenceBodyRadiusMeters;
        public double SphereOfInfluenceMeters;
        public double ApoapsisMeters;
        public double PeriapsisMeters;
        public double ApoapsisRadiusMeters;
        public double PeriapsisRadiusMeters;
        public double SemiMajorAxisMeters;
        public double SemiLatusRectumMeters;
        public double Eccentricity;
        public double InclinationDegrees;
        public double LongitudeOfAscendingNodeDegrees;
        public double ArgumentOfPeriapsisDegrees;
        public double TrueAnomalyDegrees;
        public double MeanAnomalyRadians;
        public double EpochUniversalTimeSeconds;
        public double PatchStartUniversalTimeSeconds;
        public double PatchEndUniversalTimeSeconds;
        public string PatchStartTransition;
        public string PatchEndTransition;
        public bool ActivePatch;
        public string NextPatchReferenceBody;
        public double TimeToTransitionSeconds;
        public double PeriodSeconds;
        public double TimeToApoapsisSeconds;
        public double TimeToPeriapsisSeconds;
    }
}
