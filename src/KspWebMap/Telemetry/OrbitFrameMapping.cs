using System;
using UnityEngine;

namespace KspWebMap
{
    /// <summary>
    /// Maps standard orbital-math inertial (+Z normal) to KSP reference-body local (+Y north).
    /// </summary>
    public static class OrbitFrameMapping
    {
        public static Vector3d MathInertialToKspLocal(Vector3d inertial)
        {
            return new Vector3d(inertial.x, inertial.z, inertial.y);
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
