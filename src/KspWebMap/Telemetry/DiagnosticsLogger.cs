using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspWebMap
{
    public static class DiagnosticsLogger
    {
        public const string LogPrefix = "[KspWebMap]";

        private static readonly Dictionary<string, float> LastBodyWarningTime = new Dictionary<string, float>();
        private const float BodyWarningThrottleSeconds = 10f;

        public static void LogBodyOrbitWarnings(BodyOrbitPathSnapshot[] paths, double universalTime)
        {
            if (paths == null)
            {
                return;
            }

            foreach (BodyOrbitPathSnapshot path in paths)
            {
                if (path == null || path.Validation == null || string.IsNullOrEmpty(path.BodyName))
                {
                    continue;
                }

                BodyOrbitPathValidationSnapshot validation = path.Validation;
                bool shouldWarn = false;
                string reason = null;

                if (!double.IsNaN(validation.LiveToAnalyticMeters)
                    && validation.LiveToAnalyticMeters > BodyOrbitDiagnostics.LogTrailThresholdMeters)
                {
                    shouldWarn = true;
                    reason = string.Format(
                        "liveToAnalytic={0:F0} m",
                        validation.LiveToAnalyticMeters);
                }
                else if (!double.IsNaN(validation.PeriodClosureMeters)
                    && validation.PeriodClosureMeters > BodyOrbitDiagnostics.PeriodClosureThresholdMeters)
                {
                    shouldWarn = true;
                    reason = string.Format(
                        "periodClosure={0:F0} m",
                        validation.PeriodClosureMeters);
                }
                else if (validation.TrailRenderMode == "hidden"
                    && !string.IsNullOrEmpty(validation.TrailWarning))
                {
                    shouldWarn = true;
                    reason = validation.TrailWarning;
                }

                if (!shouldWarn)
                {
                    continue;
                }

                float now = Time.realtimeSinceStartup;
                float lastTime;
                LastBodyWarningTime.TryGetValue(path.BodyName, out lastTime);

                if (now - lastTime < BodyWarningThrottleSeconds)
                {
                    continue;
                }

                LastBodyWarningTime[path.BodyName] = now;
                Debug.LogWarning(string.Format(
                    "{0} Body orbit {1} at UT {2:F0}: {3}",
                    LogPrefix,
                    path.BodyName,
                    universalTime,
                    reason));
            }
        }
    }
}
