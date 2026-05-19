namespace KspWebMap
{
    public sealed class OrbitPatchSnapshot
    {
        public int PatchIndex;
        public bool IsActivePatch;
        public string Classification;
        public string ReferenceBody;
        public string ReferenceFrame;
        public Vector3Snapshot ReferenceBodyPositionRootRelativeMeters;
        public double ReferenceBodyPositionSampleUniversalTimeSeconds;
        public string PatchPlacementMode;
        public string PatchPlacementWarning;
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
        public double TimeToPatchStartSeconds;
        public double TimeToPatchEndSeconds;
        public string PreviousPatchReferenceBody;
        public string NextPatchReferenceBody;
        public string EncounterBody;
        public string EncounterLevel;
        public double ClosestEncounterUniversalTimeSeconds;
        public double ClosestApproachMeters;
        public string CaptureWarning;
        public double PeriodSeconds;
        public double TimeToApoapsisSeconds;
        public double TimeToPeriapsisSeconds;
        public PlacementSampleSnapshot[] PlacementSamples;
    }
}
