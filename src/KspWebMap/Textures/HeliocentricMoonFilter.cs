using System.Collections.Generic;

namespace KspWebMap
{
    /// <summary>
    /// Stock moons orbiting heliocentric planets (parent orbit.referenceBody === root).
    /// </summary>
    public static class HeliocentricMoonFilter
    {
        public static bool IsHeliocentricMoon(CelestialBody body, CelestialBody rootBody)
        {
            if (body == null || rootBody == null || body == rootBody)
            {
                return false;
            }

            if (body.orbit == null)
            {
                return false;
            }

            CelestialBody parent = body.orbit.referenceBody;

            if (parent == null)
            {
                return false;
            }

            return HeliocentricPlanetFilter.IsHeliocentricPlanet(parent, rootBody);
        }

        public static List<CelestialBody> CollectHeliocentricMoons(CelestialBody rootBody)
        {
            List<CelestialBody> moons = new List<CelestialBody>();

            if (FlightGlobals.Bodies == null)
            {
                return moons;
            }

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (IsHeliocentricMoon(body, rootBody))
                {
                    moons.Add(body);
                }
            }

            return moons;
        }
    }
}
