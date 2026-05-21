using System;
using UnityEngine;

namespace KspWebMap
{
    public static class BodyOrbitDiagnostics
    {
        public const double DisplayTrailThresholdMeters = 5e4d;
        public const double LogTrailThresholdMeters = 1e6d;
        public const double PeriodClosureThresholdMeters = 5e4d;
        public const double SampleResidualThresholdMeters = 1e6d;
        public const double PlaneAngleThresholdDegrees = 0.1d;

        public static BodyOrbitPathValidationSnapshot ValidatePath(
            CelestialBody body,
            CelestialBody rootBody,
            BodyOrbitPathSnapshot path,
            double universalTime)
        {
            BodyOrbitPathValidationSnapshot validation = new BodyOrbitPathValidationSnapshot
            {
                LiveToSample0Meters = double.NaN,
                LiveToAnalyticMeters = double.NaN,
                MaxSampleToRecomputedMeters = 0d,
                MaxSampleToTrailFrameMeters = 0d,
                PeriodClosureMeters = double.NaN,
                PlaneAngleToAnalyticDegrees = double.NaN,
                TrailRenderMode = "hidden",
                TrailWarning = "Validation unavailable."
            };

            if (body == null || rootBody == null || path == null)
            {
                validation.TrailWarning = "Body, root, or path is unavailable.";
                return validation;
            }

            Vector3Snapshot live = ToSnapshot(RootRelativePositionResolver.GetBodyDisplayRootRelative(
                body,
                rootBody,
                universalTime,
                universalTime));

            if (path.Samples != null && path.Samples.Length > 0 && path.Samples[0] != null)
            {
                validation.LiveToSample0Meters = ResidualMeters(live, path.Samples[0].PositionRootRelativeMeters);
            }

            foreach (VesselRootPathSampleSnapshot sample in path.Samples ?? new VesselRootPathSampleSnapshot[0])
            {
                if (sample == null || sample.PositionRootRelativeMeters == null)
                {
                    continue;
                }

                Vector3Snapshot expectedAtSample = ToSnapshot(
                    RootRelativePositionResolver.GetBodyRootRelativeForTrailSample(
                        body,
                        rootBody,
                        sample.SampleUniversalTimeSeconds,
                        universalTime));
                double sampleResidual = ResidualMeters(sample.PositionRootRelativeMeters, expectedAtSample);
                validation.MaxSampleToRecomputedMeters = Math.Max(
                    validation.MaxSampleToRecomputedMeters,
                    sampleResidual);
                validation.MaxSampleToTrailFrameMeters = Math.Max(
                    validation.MaxSampleToTrailFrameMeters,
                    sampleResidual);
            }

            if (body.orbit != null)
            {
                double period = body.orbit.period;

                if (!double.IsNaN(period) && !double.IsInfinity(period) && period > 0d)
                {
                    // Avoid exact UT+period; KSP orbit APIs misbehave at period wrap.
                    double periodUt = universalTime + period - 1d;
                    CelestialBody parent = body.orbit.referenceBody;

                    // Moons: one period closes in parent frame, not heliocentric root frame.
                    if (parent != null && parent != body && parent != rootBody)
                    {
                        try
                        {
                            Vector3d atStart = body.orbit.getRelativePositionAtUT(universalTime);
                            Vector3d atPeriod = body.orbit.getRelativePositionAtUT(periodUt);
                            validation.PeriodClosureMeters = (atStart - atPeriod).magnitude;
                        }
                        catch
                        {
                            validation.PeriodClosureMeters = double.NaN;
                        }
                    }
                    else
                    {
                        Vector3d atStart;
                        Vector3d atPeriod;

                        if (RootRelativePositionResolver.TryGetTrueRootRelative(
                            body,
                            rootBody,
                            universalTime,
                            out atStart)
                            && RootRelativePositionResolver.TryGetTrueRootRelative(
                            body,
                            rootBody,
                            periodUt,
                            out atPeriod))
                        {
                            validation.PeriodClosureMeters = (atStart - atPeriod).magnitude;
                        }
                        else
                        {
                            atStart = RootRelativePositionResolver.GetBodyRootRelative(
                                body,
                                rootBody,
                                universalTime,
                                universalTime);
                            atPeriod = RootRelativePositionResolver.GetBodyRootRelative(
                                body,
                                rootBody,
                                periodUt,
                                universalTime);
                            validation.PeriodClosureMeters = (atStart - atPeriod).magnitude;
                        }
                    }
                }
            }

            validation.LiveToAnalyticMeters = ComputeLiveToAnalyticMeters(
                live,
                path.OrbitElements,
                rootBody,
                body,
                universalTime);
            validation.PlaneNormalRootRelative = ComputePlaneNormalFromSamples(path.Samples);
            validation.PlaneAngleToAnalyticDegrees = ComputePlaneAngleToAnalytic(
                validation.PlaneNormalRootRelative,
                path.OrbitElements);

            validation.TrailRenderMode = DetermineTrailRenderMode(validation);
            validation.TrailWarning = BuildTrailWarning(validation);
            return validation;
        }

        private static string DetermineTrailRenderMode(BodyOrbitPathValidationSnapshot validation)
        {
            if (!double.IsNaN(validation.LiveToAnalyticMeters)
                && validation.LiveToAnalyticMeters <= DisplayTrailThresholdMeters)
            {
                return "analytic";
            }

            if (!double.IsNaN(validation.MaxSampleToTrailFrameMeters)
                && validation.MaxSampleToTrailFrameMeters <= SampleResidualThresholdMeters
                && !double.IsNaN(validation.PeriodClosureMeters)
                && validation.PeriodClosureMeters <= PeriodClosureThresholdMeters)
            {
                return "samples";
            }

            return "hidden";
        }

        private static string BuildTrailWarning(BodyOrbitPathValidationSnapshot validation)
        {
            if (validation.TrailRenderMode == "analytic")
            {
                return null;
            }

            if (validation.TrailRenderMode == "samples")
            {
                return null;
            }

            if (!double.IsNaN(validation.MaxSampleToTrailFrameMeters)
                && validation.MaxSampleToTrailFrameMeters > SampleResidualThresholdMeters)
            {
                return "Trail samples diverge from true position; trail hidden.";
            }

            if (!double.IsNaN(validation.MaxSampleToRecomputedMeters)
                && validation.MaxSampleToRecomputedMeters > SampleResidualThresholdMeters)
            {
                return "Propagation residual exceeds threshold; trail hidden.";
            }

            if (!double.IsNaN(validation.PeriodClosureMeters)
                && validation.PeriodClosureMeters > PeriodClosureThresholdMeters)
            {
                return "Period closure failed; trail hidden.";
            }

            if (!double.IsNaN(validation.LiveToAnalyticMeters)
                && validation.LiveToAnalyticMeters > DisplayTrailThresholdMeters)
            {
                return "Analytic trail does not match live body position; trail hidden.";
            }

            return "Trail hidden due to validation failure.";
        }

        private static double ComputeLiveToAnalyticMeters(
            Vector3Snapshot live,
            BodyOrbitElementsSnapshot elements,
            CelestialBody rootBody,
            CelestialBody body,
            double universalTime)
        {
            if (live == null || elements == null)
            {
                return double.NaN;
            }

            string referenceName = elements.ReferenceBody;
            Vector3d anchor = Vector3d.zero;

            if (!string.IsNullOrEmpty(referenceName)
                && rootBody != null
                && referenceName != rootBody.bodyName)
            {
                CelestialBody referenceBody = FindBodyByName(referenceName);

                if (referenceBody != null)
                {
                    anchor = RootRelativePositionResolver.GetBodyDisplayRootRelative(
                        referenceBody,
                        rootBody,
                        universalTime,
                        universalTime);
                }
            }

            double best = double.MaxValue;
            double eccentricity = elements.Eccentricity;
            double semiMajorAxis = elements.SemiMajorAxisMeters;
            double semiLatusRectum = elements.SemiLatusRectumMeters;

            if (double.IsNaN(semiLatusRectum) || semiLatusRectum <= 0d)
            {
                if (double.IsNaN(semiMajorAxis) || double.IsNaN(eccentricity))
                {
                    return double.NaN;
                }

                if (eccentricity < 1d)
                {
                    semiLatusRectum = semiMajorAxis * (1d - eccentricity * eccentricity);
                }
                else if (eccentricity > 1d)
                {
                    semiLatusRectum = Math.Abs(semiMajorAxis) * (eccentricity * eccentricity - 1d);
                }
            }

            if (semiLatusRectum <= 0d || double.IsNaN(eccentricity))
            {
                return double.NaN;
            }

            double startTrueAnomalyRadians = 0d;

            if (!double.IsNaN(elements.TrueAnomalyDegreesAtCapture))
            {
                startTrueAnomalyRadians = elements.TrueAnomalyDegreesAtCapture * Math.PI / 180d;
            }

            for (int i = 0; i < 360; i++)
            {
                double trueAnomaly = startTrueAnomalyRadians + i / 360d * Math.PI * 2d;
                double radius = semiLatusRectum / (1d + eccentricity * Math.Cos(trueAnomaly));
                Vector3d perifocal = new Vector3d(
                    radius * Math.Cos(trueAnomaly),
                    radius * Math.Sin(trueAnomaly),
                    0d);
                Vector3d inertial = PerifocalToInertial(perifocal, elements);
                Vector3d kspLocal = OrbitFrameMapping.MathInertialToKspLocal(inertial);
                Vector3d rootPoint = anchor + kspLocal;
                Vector3d liveVector = new Vector3d(live.X, live.Y, live.Z);
                best = Math.Min(best, (rootPoint - liveVector).magnitude);
            }

            return best;
        }

        private static Vector3Snapshot ComputePlaneNormalFromSamples(VesselRootPathSampleSnapshot[] samples)
        {
            if (samples == null || samples.Length < 3)
            {
                return null;
            }

            Vector3Snapshot a = null;
            Vector3Snapshot b = null;
            Vector3Snapshot c = null;

            foreach (VesselRootPathSampleSnapshot sample in samples)
            {
                if (sample == null || sample.PositionRootRelativeMeters == null)
                {
                    continue;
                }

                if (a == null)
                {
                    a = sample.PositionRootRelativeMeters;
                    continue;
                }

                if (b == null && !AreSamePoint(a, sample.PositionRootRelativeMeters))
                {
                    b = sample.PositionRootRelativeMeters;
                    continue;
                }

                if (b != null && c == null && !AreSamePoint(a, sample.PositionRootRelativeMeters)
                    && !AreSamePoint(b, sample.PositionRootRelativeMeters))
                {
                    c = sample.PositionRootRelativeMeters;
                    break;
                }
            }

            if (a == null || b == null || c == null)
            {
                return null;
            }

            Vector3d v1 = new Vector3d(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            Vector3d v2 = new Vector3d(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
            Vector3d normal = Vector3d.Cross(v1, v2);

            if (normal.sqrMagnitude <= 0d)
            {
                return null;
            }

            normal = normal.normalized;
            return ToSnapshot(normal);
        }

        private static double ComputePlaneAngleToAnalytic(
            Vector3Snapshot samplePlaneNormal,
            BodyOrbitElementsSnapshot elements)
        {
            if (samplePlaneNormal == null || elements == null)
            {
                return double.NaN;
            }

            Vector3d analyticNormal = OrbitFrameMapping.MathInertialToKspLocal(
                PerifocalToInertial(new Vector3d(0d, 0d, 1d), elements));

            if (analyticNormal.sqrMagnitude <= 0d)
            {
                return double.NaN;
            }

            analyticNormal = analyticNormal.normalized;
            Vector3d sampleNormal = new Vector3d(
                samplePlaneNormal.X,
                samplePlaneNormal.Y,
                samplePlaneNormal.Z).normalized;
            double dot = Math.Abs(Vector3d.Dot(sampleNormal, analyticNormal));
            dot = Math.Min(1d, Math.Max(-1d, dot));
            return Math.Acos(dot) * 180d / Math.PI;
        }

        private static Vector3d PerifocalToInertial(Vector3d point, BodyOrbitElementsSnapshot elements)
        {
            double lan = elements.LongitudeOfAscendingNodeDegrees * Math.PI / 180d;
            double inclination = elements.InclinationDegrees * Math.PI / 180d;
            double argumentOfPeriapsis = elements.ArgumentOfPeriapsisDegrees * Math.PI / 180d;
            double cosW = Math.Cos(argumentOfPeriapsis);
            double sinW = Math.Sin(argumentOfPeriapsis);
            double cosI = Math.Cos(inclination);
            double sinI = Math.Sin(inclination);
            double cosO = Math.Cos(lan);
            double sinO = Math.Sin(lan);
            double x1 = cosW * point.x - sinW * point.y;
            double y1 = sinW * point.x + cosW * point.y;
            double x2 = x1;
            double y2 = cosI * y1;
            double z2 = sinI * y1;
            return new Vector3d(
                cosO * x2 - sinO * y2,
                sinO * x2 + cosO * y2,
                z2);
        }

        private static bool AreSamePoint(Vector3Snapshot a, Vector3Snapshot b)
        {
            return Math.Abs(a.X - b.X) < 1d
                && Math.Abs(a.Y - b.Y) < 1d
                && Math.Abs(a.Z - b.Z) < 1d;
        }

        private static double ResidualMeters(Vector3Snapshot a, Vector3Snapshot b)
        {
            if (a == null || b == null)
            {
                return double.NaN;
            }

            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
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
    }
}
