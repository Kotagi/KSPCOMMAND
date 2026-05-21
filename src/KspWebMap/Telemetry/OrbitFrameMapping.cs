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
    }
}
