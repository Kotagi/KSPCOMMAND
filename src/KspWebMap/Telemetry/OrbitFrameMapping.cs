using System;
using UnityEngine;

namespace KspWebMap
{
    /// <summary>
    /// Maps standard orbital-math inertial (+Z normal) to KSP reference-body local (+Y north).
    /// </summary>
    public static class OrbitFrameMapping
    {
        /// <summary>
        /// Unity world → root-relative axes used by getRelativePositionAtUT (Sun-child display).
        /// Matches MathInertialToKspLocal: (x,y,z)_world → (x,z,y)_root.
        /// </summary>
        private static readonly QuaternionD WorldToRootRelativeRotation = CreateRotationAboutX(Math.PI / 2d);

        public static Vector3d MathInertialToKspLocal(Vector3d inertial)
        {
            return new Vector3d(inertial.x, inertial.z, inertial.y);
        }

        public static Vector3d WorldVectorToRootRelativeFrame(Vector3d world)
        {
            return WorldToRootRelativeRotation * world;
        }

        /// <summary>
        /// World body.rotation composed with the same +90° X frame used for root-relative vectors.
        /// Matches the web mesh pipeline (not a passive similarity transform).
        /// </summary>
        public static QuaternionD WorldRotationToRootRelativeFrame(QuaternionD world)
        {
            return WorldToRootRelativeRotation * world;
        }

        private static QuaternionD CreateRotationAboutX(double angleRadians)
        {
            double half = angleRadians * 0.5d;
            return new QuaternionD(
                Math.Sin(half),
                0d,
                0d,
                Math.Cos(half));
        }

        /// <summary>Unit orbital angular momentum in KSP root-local axes from Kepler elements.</summary>
        public static Vector3d OrbitNormalKspLocal(BodyOrbitElementsSnapshot elements)
        {
            if (elements == null)
            {
                return Vector3d.zero;
            }

            double lan = elements.LongitudeOfAscendingNodeDegrees * Math.PI / 180d;
            double inclination = elements.InclinationDegrees * Math.PI / 180d;
            double sinO = Math.Sin(lan);
            double cosO = Math.Cos(lan);
            double sinI = Math.Sin(inclination);
            double cosI = Math.Cos(inclination);
            Vector3d mathInertial = new Vector3d(sinO * sinI, -cosO * sinI, cosI);
            return MathInertialToKspLocal(mathInertial);
        }
    }
}
