using System;
using UnityEngine;

namespace KspWebMap
{
    public enum OrbitOffsetMode
    {
        NoFlipRelative = 0,
        FlipRelative = 1
    }

    /// <summary>
    /// Single authority for root-relative positions in the solar-system frame.
    /// Live positions use body.position; other UTs use parent chain + getRelativePositionAtUT.
    /// </summary>
    public static class RootRelativePositionResolver
    {
        public const string ResolverVersion = "2";
        public const double LiveToleranceSeconds = 1d;

        private static OrbitOffsetMode _orbitOffsetMode = OrbitOffsetMode.NoFlipRelative;
        private static OrbitOffsetMode _vesselOffsetMode = OrbitOffsetMode.NoFlipRelative;
        private static bool _calibrated;
        private static bool _vesselOffsetCalibrated;

        public static OrbitOffsetMode OrbitOffsetMode
        {
            get { return _orbitOffsetMode; }
        }

        public static string OrbitOffsetModeName
        {
            get
            {
                return _orbitOffsetMode == OrbitOffsetMode.FlipRelative
                    ? "flipRelative"
                    : "noFlipRelative";
            }
        }

        public static string VesselOffsetModeName
        {
            get
            {
                return _vesselOffsetMode == OrbitOffsetMode.FlipRelative
                    ? "flipRelative"
                    : "noFlipRelative";
            }
        }

        public static void ResetCalibration()
        {
            _calibrated = false;
            _vesselOffsetCalibrated = false;
            _orbitOffsetMode = OrbitOffsetMode.NoFlipRelative;
            _vesselOffsetMode = OrbitOffsetMode.NoFlipRelative;
        }

        public static void EnsureCalibrated(CelestialBody rootBody, double universalTime)
        {
            if (_calibrated || rootBody == null || FlightGlobals.Bodies == null)
            {
                return;
            }

            double flipTotal = 0d;
            double noFlipTotal = 0d;
            int count = 0;
            string[] calibrationBodies = { "Kerbin", "Mun", "Minmus", "Moho", "Eve", "Gilly", "Duna", "Ike", "Laythe", "Tylo" };

            foreach (string bodyName in calibrationBodies)
            {
                CelestialBody body = FindBodyByName(bodyName);

                if (body == null || body.orbit == null || body == rootBody)
                {
                    continue;
                }

                Vector3d liveNow = GetLiveRootRelativeInternal(body, rootBody);
                Vector3d flipNow = PropagateRootRelative(
                    body,
                    rootBody,
                    universalTime,
                    universalTime,
                    OrbitOffsetMode.FlipRelative);
                Vector3d noFlipNow = PropagateRootRelative(
                    body,
                    rootBody,
                    universalTime,
                    universalTime,
                    OrbitOffsetMode.NoFlipRelative);

                flipTotal += (liveNow - flipNow).magnitude;
                noFlipTotal += (liveNow - noFlipNow).magnitude;
                count++;

                double period = body.orbit.period;
                double[] testOffsetsSeconds = { 60d };

                if (!double.IsNaN(period) && !double.IsInfinity(period) && period > 0d)
                {
                    testOffsetsSeconds = new double[] { 60d, period / 48d };
                }

                foreach (double offsetSeconds in testOffsetsSeconds)
                {
                    double testUt = universalTime + offsetSeconds;
                    Vector3d reference;

                    if (!TryGetTrueRootRelative(body, rootBody, testUt, out reference))
                    {
                        continue;
                    }

                    Vector3d flip = PropagateRootRelative(
                        body,
                        rootBody,
                        testUt,
                        universalTime,
                        OrbitOffsetMode.FlipRelative);
                    Vector3d noFlip = PropagateRootRelative(
                        body,
                        rootBody,
                        testUt,
                        universalTime,
                        OrbitOffsetMode.NoFlipRelative);

                    flipTotal += (reference - flip).magnitude;
                    noFlipTotal += (reference - noFlip).magnitude;
                    count++;
                }
            }

            if (count > 0)
            {
                _orbitOffsetMode = noFlipTotal <= flipTotal
                    ? OrbitOffsetMode.NoFlipRelative
                    : OrbitOffsetMode.FlipRelative;
            }

            _calibrated = true;
        }

        /// <summary>
        /// KSP true world positions differenced into root frame (calibration reference only).
        /// </summary>
        public static bool TryGetTrueRootRelative(
            CelestialBody body,
            CelestialBody rootBody,
            double universalTime,
            out Vector3d rootRelative)
        {
            rootRelative = Vector3d.zero;

            if (body == null || rootBody == null || !IsValidUniversalTime(universalTime))
            {
                return false;
            }

            if (body == rootBody)
            {
                return true;
            }

            try
            {
                Vector3d bodyWorld = GetTrueWorldPosition(body, universalTime);
                Vector3d rootWorld = GetRootWorldPositionAtUniversalTime(rootBody, universalTime);
                rootRelative = bodyWorld - rootWorld;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Trail samples aligned with live icons: live at capture UT, heliocentric true for
        /// bodies orbiting the root, parent-chain relative offsets for moons.
        /// </summary>
        /// <summary>
        /// Display authority: icons, ephemeris chain bodies, and trail sample 0 at capture UT.
        /// </summary>
        public static Vector3d GetBodyDisplayRootRelative(
            CelestialBody body,
            CelestialBody rootBody,
            double universalTime,
            double currentUniversalTime)
        {
            return GetBodyRootRelativeForTrailSample(
                body,
                rootBody,
                universalTime,
                currentUniversalTime);
        }

        public static Vector3d GetLiveRootRelative(CelestialBody body, CelestialBody rootBody)
        {
            return GetLiveRootRelativeInternal(body, rootBody);
        }

        public static Vector3d GetBodyRootRelativeForTrailSample(
            CelestialBody body,
            CelestialBody rootBody,
            double sampleUniversalTime,
            double currentUniversalTime)
        {
            if (body == null || rootBody == null)
            {
                return Vector3d.zero;
            }

            if (body == rootBody)
            {
                return Vector3d.zero;
            }

            if (IsValidUniversalTime(currentUniversalTime)
                && Math.Abs(sampleUniversalTime - currentUniversalTime) <= LiveToleranceSeconds)
            {
                return GetLiveRootRelativeInternal(body, rootBody);
            }

            if (body.orbit == null)
            {
                return GetLiveRootRelativeInternal(body, rootBody);
            }

            CelestialBody parent = body.orbit.referenceBody;

            if (parent == null || parent == body)
            {
                return ApplyOrbitOffset(
                    body.orbit.getRelativePositionAtUT(sampleUniversalTime),
                    _orbitOffsetMode);
            }

            if (parent == rootBody)
            {
                Vector3d heliocentricTrue;

                if (TryGetHeliocentricTrueRootRelative(body, rootBody, sampleUniversalTime, out heliocentricTrue))
                {
                    return heliocentricTrue;
                }

                return ApplyOrbitOffset(
                    body.orbit.getRelativePositionAtUT(sampleUniversalTime),
                    _orbitOffsetMode);
            }

            Vector3d parentRootRelative = GetBodyRootRelativeForTrailSample(
                parent,
                rootBody,
                sampleUniversalTime,
                currentUniversalTime);
            return parentRootRelative
                + ApplyOrbitOffset(body.orbit.getRelativePositionAtUT(sampleUniversalTime), _orbitOffsetMode);
        }

        /// <summary>
        /// Body orbiting the root (e.g. Kerbin around Sun) without world parent stacking.
        /// </summary>
        private static bool TryGetHeliocentricTrueRootRelative(
            CelestialBody body,
            CelestialBody rootBody,
            double universalTime,
            out Vector3d rootRelative)
        {
            rootRelative = Vector3d.zero;

            if (body == null || rootBody == null || body.orbit == null || !IsValidUniversalTime(universalTime))
            {
                return false;
            }

            try
            {
                Vector3d bodyWorld = body.orbit.getTruePositionAtUT(universalTime);
                Vector3d rootWorld = GetRootWorldPositionAtUniversalTime(rootBody, universalTime);
                rootRelative = bodyWorld - rootWorld;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Vector3d GetBodyRootRelative(
            CelestialBody body,
            CelestialBody rootBody,
            double universalTime,
            double currentUniversalTime)
        {
            if (body == null || rootBody == null)
            {
                return Vector3d.zero;
            }

            if (body == rootBody)
            {
                return Vector3d.zero;
            }

            if (IsValidUniversalTime(currentUniversalTime)
                && Math.Abs(universalTime - currentUniversalTime) <= LiveToleranceSeconds)
            {
                return GetLiveRootRelativeInternal(body, rootBody);
            }

            if (body.orbit == null)
            {
                return GetLiveRootRelativeInternal(body, rootBody);
            }

            return PropagateRootRelative(
                body,
                rootBody,
                universalTime,
                currentUniversalTime,
                _orbitOffsetMode);
        }

        /// <summary>
        /// Calibrate vessel getRelativePositionAtUT offset against live world position at capture UT.
        /// </summary>
        public static void EnsureVesselOffsetCalibrated(
            Vessel vessel,
            Orbit orbit,
            CelestialBody rootBody,
            double universalTime)
        {
            if (_vesselOffsetCalibrated || vessel == null || orbit == null || rootBody == null)
            {
                return;
            }

            if (!IsValidUniversalTime(universalTime))
            {
                return;
            }

            Vector3d liveRootRelative = vessel.GetWorldPos3D() - rootBody.position;
            CelestialBody referenceBody = orbit.referenceBody;
            Vector3d referenceRootRelative = Vector3d.zero;

            if (referenceBody != null)
            {
                referenceRootRelative = GetBodyRootRelativeForTrailSample(
                    referenceBody,
                    rootBody,
                    universalTime,
                    universalTime);
            }

            Vector3d relativeAtUt = orbit.getRelativePositionAtUT(universalTime);
            Vector3d flipCandidate = referenceRootRelative
                + ApplyOrbitOffset(relativeAtUt, OrbitOffsetMode.FlipRelative);
            Vector3d noFlipCandidate = referenceRootRelative
                + ApplyOrbitOffset(relativeAtUt, OrbitOffsetMode.NoFlipRelative);

            _vesselOffsetMode = (liveRootRelative - noFlipCandidate).magnitude
                <= (liveRootRelative - flipCandidate).magnitude
                ? OrbitOffsetMode.NoFlipRelative
                : OrbitOffsetMode.FlipRelative;
            _vesselOffsetCalibrated = true;
        }

        /// <summary>
        /// Vessel path samples: live at now; otherwise true solar position minus root at UT.
        /// Falls back to reference-body chain when true propagation is unavailable.
        /// </summary>
        public static Vector3d GetVesselRootRelativeForTrailSample(
            Orbit orbit,
            CelestialBody rootBody,
            double universalTime,
            double currentUniversalTime,
            Vessel vessel)
        {
            if (orbit == null || rootBody == null)
            {
                return Vector3d.zero;
            }

            if (vessel != null
                && IsValidUniversalTime(currentUniversalTime)
                && Math.Abs(universalTime - currentUniversalTime) <= LiveToleranceSeconds)
            {
                return vessel.GetWorldPos3D() - rootBody.position;
            }

            Vector3d trueRootRelative;

            if (TryGetVesselTrueRootRelative(orbit, rootBody, universalTime, out trueRootRelative))
            {
                return trueRootRelative;
            }

            CelestialBody referenceBody = orbit.referenceBody;

            if (referenceBody == null || referenceBody == rootBody)
            {
                return ApplyOrbitOffset(orbit.getRelativePositionAtUT(universalTime), _vesselOffsetMode);
            }

            Vector3d referenceRootRelative = GetBodyRootRelativeForTrailSample(
                referenceBody,
                rootBody,
                universalTime,
                currentUniversalTime);
            return referenceRootRelative
                + ApplyOrbitOffset(orbit.getRelativePositionAtUT(universalTime), _vesselOffsetMode);
        }

        private static bool TryGetVesselTrueRootRelative(
            Orbit orbit,
            CelestialBody rootBody,
            double universalTime,
            out Vector3d rootRelative)
        {
            rootRelative = Vector3d.zero;

            if (orbit == null || rootBody == null || !IsValidUniversalTime(universalTime))
            {
                return false;
            }

            try
            {
                Vector3d vesselWorld = orbit.getTruePositionAtUT(universalTime);
                Vector3d rootWorld = GetRootWorldPositionAtUniversalTime(rootBody, universalTime);
                rootRelative = vesselWorld - rootWorld;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Vector3d GetVesselRootRelative(
            Orbit orbit,
            CelestialBody rootBody,
            double universalTime,
            double currentUniversalTime)
        {
            return GetVesselRootRelativeForTrailSample(
                orbit,
                rootBody,
                universalTime,
                currentUniversalTime,
                null);
        }

        private static Vector3d PropagateRootRelative(
            CelestialBody body,
            CelestialBody rootBody,
            double universalTime,
            double currentUniversalTime,
            OrbitOffsetMode mode)
        {
            if (body == null || rootBody == null)
            {
                return Vector3d.zero;
            }

            if (body == rootBody)
            {
                return Vector3d.zero;
            }

            if (body.orbit == null)
            {
                return GetLiveRootRelativeInternal(body, rootBody);
            }

            CelestialBody parent = body.orbit.referenceBody;

            if (parent == null || parent == body)
            {
                return ApplyOrbitOffset(body.orbit.getRelativePositionAtUT(universalTime), mode);
            }

            Vector3d parentRootRelative = PropagateRootRelative(
                parent,
                rootBody,
                universalTime,
                currentUniversalTime,
                mode);
            return parentRootRelative
                + ApplyOrbitOffset(body.orbit.getRelativePositionAtUT(universalTime), mode);
        }

        private static Vector3d GetLiveRootRelativeInternal(CelestialBody body, CelestialBody rootBody)
        {
            return body.position - rootBody.position;
        }

        private static Vector3d ApplyOrbitOffset(Vector3d value, OrbitOffsetMode mode)
        {
            if (mode == OrbitOffsetMode.FlipRelative)
            {
                return new Vector3d(value.x, -value.y, -value.z);
            }

            return value;
        }

        private static bool IsValidUniversalTime(double universalTime)
        {
            return !double.IsNaN(universalTime) && !double.IsInfinity(universalTime);
        }

        private static Vector3d GetTrueWorldPosition(CelestialBody body, double universalTime)
        {
            return GetRootWorldPositionAtUniversalTime(body, universalTime);
        }

        private static Vector3d GetRootWorldPositionAtUniversalTime(CelestialBody body, double universalTime)
        {
            if (body == null)
            {
                return Vector3d.zero;
            }

            if (body.orbit != null)
            {
                return body.orbit.getTruePositionAtUT(universalTime);
            }

            return body.position;
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
