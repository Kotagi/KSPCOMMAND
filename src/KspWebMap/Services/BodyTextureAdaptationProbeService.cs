using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KspWebMap
{
    /// <summary>
    /// Repeatedly probes ScaledSpace materials in flight to discover RSS/mod-compatible
    /// albedo extraction strategies. Logs to KSP.log and writes preview JPEGs under
    /// Web/assets/bodies/_probe/.
    /// </summary>
    public sealed class BodyTextureAdaptationProbeService : IKspWebMapService
    {
        private const string LogPrefix = "[KspWebMap][TextureProbe]";
        private const float InitialDelaySeconds = 8f;
        private const float PassIntervalSeconds = 20f;
        private const int MaxPasses = 9;

        private readonly GameObject _host;
        private readonly ServerConfig _config;
        private BodyTextureAdaptationProbeComponent _component;

        public BodyTextureAdaptationProbeService(GameObject host, ServerConfig config)
        {
            _host = host;
            _config = config;
        }

        public string Name
        {
            get { return "Body Texture Adaptation Probe"; }
        }

        public void Start()
        {
            if (_component == null)
            {
                _component = _host.AddComponent<BodyTextureAdaptationProbeComponent>();
                _component.Initialize(_config);
            }
        }

        public void Stop()
        {
            if (_component != null)
            {
                UnityEngine.Object.Destroy(_component);
                _component = null;
            }
        }

        private sealed class BodyTextureAdaptationProbeComponent : MonoBehaviour
        {
            private ServerConfig _config;
            private string _probeDirectory;
            private readonly Dictionary<string, string> _bestStrategyByBody =
                new Dictionary<string, string>(StringComparer.Ordinal);

            public void Initialize(ServerConfig config)
            {
                _config = config;
                _probeDirectory = Path.Combine(_config.WebRoot, "assets", "bodies", "_probe");
                Directory.CreateDirectory(_probeDirectory);
                StartCoroutine(RunProbeLoop());
            }

            private IEnumerator RunProbeLoop()
            {
                yield return new WaitForSeconds(InitialDelaySeconds);

                for (int pass = 0; pass < MaxPasses; pass++)
                {
                    RunPass(pass);

                    if (pass + 1 < MaxPasses)
                    {
                        yield return new WaitForSeconds(PassIntervalSeconds);
                    }
                }

                WriteSummary();
                UnityEngine.Debug.Log(LogPrefix + " probe loop complete after " + MaxPasses + " passes.");
            }

            private void RunPass(int passIndex)
            {
                CelestialBody rootBody = HeliocentricPlanetFilter.FindRootBody();
                List<CelestialBody> planets = HeliocentricPlanetFilter.CollectHeliocentricPlanets(rootBody);
                List<CelestialBody> moons = HeliocentricMoonFilter.CollectHeliocentricMoons(rootBody);
                float ut = (float)Planetarium.GetUniversalTime();

                UnityEngine.Debug.Log(string.Format(
                    "{0} pass {1}/{2} ut={3:F1} probing {4} planets + {5} moons. previews={6}",
                    LogPrefix,
                    passIndex + 1,
                    MaxPasses,
                    ut,
                    planets.Count,
                    moons.Count,
                    _probeDirectory));

                ProbeBodies(planets, passIndex, ut, "planet");
                ProbeBodies(moons, passIndex, ut, "moon");
            }

            private void ProbeBodies(
                List<CelestialBody> bodies,
                int passIndex,
                float ut,
                string kind)
            {
                for (int i = 0; i < bodies.Count; i++)
                {
                    CelestialBody body = bodies[i];
                    if (body == null)
                    {
                        continue;
                    }

                    BodyTextureAlbedoStrategies.BodyProbeReport report =
                        BodyTextureAlbedoStrategies.ProbeBody(body, passIndex, ut, _probeDirectory);

                    string formatted = BodyTextureAlbedoStrategies.FormatReport(report);
                    UnityEngine.Debug.Log(LogPrefix + " " + kind + " " + formatted);

                    TrackBestStrategy(report);
                }
            }

            private void TrackBestStrategy(BodyTextureAlbedoStrategies.BodyProbeReport report)
            {
                if (report.BestAttempt == null || !report.BestAttempt.CaptureSucceeded)
                {
                    return;
                }

                string signature = report.BestAttempt.StrategyId
                    + "|R" + report.BestAttempt.RendererIndex
                    + "|" + (report.BestAttempt.PropertyName ?? string.Empty);

                string previous;
                if (_bestStrategyByBody.TryGetValue(report.BodyName, out previous))
                {
                    if (previous == signature)
                    {
                        return;
                    }

                    UnityEngine.Debug.Log(LogPrefix + " strategy changed for "
                        + report.BodyName + ": " + previous + " -> " + signature);
                }
                else
                {
                    UnityEngine.Debug.Log(LogPrefix + " first working strategy for "
                        + report.BodyName + ": " + signature
                        + " preview=" + report.BestAttempt.PreviewPath);
                }

                _bestStrategyByBody[report.BodyName] = signature;
            }

            private void WriteSummary()
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("KspWebMap texture adaptation probe summary");
                builder.AppendLine("Generated at UT " + Planetarium.GetUniversalTime().ToString("F1"));
                builder.AppendLine("Preview directory: " + _probeDirectory);
                builder.AppendLine();

                if (_bestStrategyByBody.Count == 0)
                {
                    builder.AppendLine("No successful capture strategies found.");
                    builder.AppendLine("Check KSP.log for [KspWebMap][TextureProbe] material property listings.");
                }
                else
                {
                    builder.AppendLine("Bodies with at least one successful capture:");
                    foreach (KeyValuePair<string, string> entry in _bestStrategyByBody)
                    {
                        builder.AppendLine("  " + entry.Key + " => " + entry.Value);
                    }
                }

                string summaryPath = Path.Combine(_probeDirectory, "summary.txt");
                File.WriteAllText(summaryPath, builder.ToString());
                UnityEngine.Debug.Log(LogPrefix + " wrote summary: " + summaryPath);
            }
        }
    }
}
