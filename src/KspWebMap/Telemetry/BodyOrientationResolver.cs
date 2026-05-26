using UnityEngine;

namespace KspWebMap
{
    /// <summary>
    /// Body-fixed → root-relative orientation in the same axes as
    /// RootRelativePositionResolver / getRelativePositionAtUT (not raw Unity world).
    /// </summary>
    public static class BodyOrientationResolver
    {
        public const string OrientationReferenceFrame = "solarSystemRootCenteredInertial";

        public static void ApplyToSnapshot(
            CelestialBodySnapshot snapshot,
            CelestialBody body,
            double universalTime)
        {
            if (snapshot == null || body == null)
            {
                return;
            }

            snapshot.BodyOrientationReferenceFrame = OrientationReferenceFrame;
            snapshot.BodyOrientationSampleUniversalTimeSeconds = universalTime;
            snapshot.Rotates = body.rotates;
            snapshot.InverseRotation = body.inverseRotation;
            snapshot.TidallyLocked = body.tidallyLocked;
            snapshot.RotationPeriodSeconds = body.rotationPeriod;
            snapshot.RotationAngleRadians = body.rotationAngle;

            if (!body.rotates || body.rotationPeriod <= 0d)
            {
                snapshot.BodyOrientationRootRelative = IdentityQuaternion();
                snapshot.SpinAxisRootRelative = ToSnapshot(new Vector3d(0d, 1d, 0d));
                snapshot.AngularVelocityRootRelativeRadPerSec = ToSnapshot(Vector3d.zero);
                return;
            }

            QuaternionD bodyRootFrame = OrbitFrameMapping.WorldRotationToRootRelativeFrame(body.rotation);
            snapshot.BodyOrientationRootRelative = ToSnapshot(bodyRootFrame);

            Vector3d angularVelocity = OrbitFrameMapping.WorldVectorToRootRelativeFrame(body.angularVelocity);
            snapshot.AngularVelocityRootRelativeRadPerSec = ToSnapshot(angularVelocity);

            Vector3d north = bodyRootFrame * new Vector3d(0d, 1d, 0d);
            double northMag = north.magnitude;
            if (northMag > 1e-12)
            {
                snapshot.SpinAxisRootRelative = ToSnapshot(north / northMag);
            }
            else
            {
                snapshot.SpinAxisRootRelative = ToSnapshot(new Vector3d(0d, 1d, 0d));
            }
        }

        private static QuaternionSnapshot IdentityQuaternion()
        {
            return new QuaternionSnapshot { X = 0d, Y = 0d, Z = 0d, W = 1d };
        }

        private static QuaternionSnapshot ToSnapshot(QuaternionD q)
        {
            return new QuaternionSnapshot
            {
                X = q.x,
                Y = q.y,
                Z = q.z,
                W = q.w
            };
        }

        private static Vector3Snapshot ToSnapshot(Vector3d v)
        {
            return new Vector3Snapshot
            {
                X = v.x,
                Y = v.y,
                Z = v.z
            };
        }
    }
}
