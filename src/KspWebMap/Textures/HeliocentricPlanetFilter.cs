using System.Collections.Generic;

namespace KspWebMap
{
    public static class HeliocentricPlanetFilter
    {
        public static bool IsHeliocentricPlanet(CelestialBody body, CelestialBody rootBody)
        {
            if (body == null || rootBody == null || body == rootBody)
            {
                return false;
            }

            if (body.orbit == null)
            {
                return false;
            }

            return body.orbit.referenceBody == rootBody;
        }

        public static List<CelestialBody> CollectHeliocentricPlanets(CelestialBody rootBody)
        {
            List<CelestialBody> planets = new List<CelestialBody>();

            if (FlightGlobals.Bodies == null)
            {
                return planets;
            }

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (IsHeliocentricPlanet(body, rootBody))
                {
                    planets.Add(body);
                }
            }

            return planets;
        }

        public static CelestialBody FindRootBody()
        {
            if (FlightGlobals.Bodies == null)
            {
                return null;
            }

            CelestialBody namedSun = null;
            CelestialBody firstBody = null;

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body == null)
                {
                    continue;
                }

                if (firstBody == null)
                {
                    firstBody = body;
                }

                if (body.referenceBody == body)
                {
                    namedSun = body;
                }
            }

            return namedSun != null ? namedSun : firstBody;
        }
    }
}
