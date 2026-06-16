using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KspWebMap
{
    public sealed class BodyTextureExportService : IKspWebMapService
    {
        private const string LogPrefix = "[KspWebMap]";
        private const double ExportBudgetWarningSeconds = 5d;

        private readonly ServerConfig _config;
        private readonly BodyTextureExportRegistry _registry;
        private readonly GameObject _host;
        private BodyTextureExportComponent _component;

        public BodyTextureExportService(
            GameObject host,
            ServerConfig config,
            BodyTextureExportRegistry registry)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (registry == null)
            {
                throw new ArgumentNullException("registry");
            }

            _host = host;
            _config = config;
            _registry = registry;
        }

        public string Name
        {
            get { return "Body Texture Export"; }
        }

        public void Start()
        {
            if (_component == null)
            {
                _component = _host.AddComponent<BodyTextureExportComponent>();
                _component.Initialize(_config, _registry);
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

        private sealed class BodyTextureExportComponent : MonoBehaviour
        {
            private ServerConfig _config;
            private BodyTextureExportRegistry _registry;
            private string _outputDirectory;
            private bool _started;

            public void Initialize(ServerConfig config, BodyTextureExportRegistry registry)
            {
                _config = config;
                _registry = registry;
                _outputDirectory = Path.Combine(config.WebRoot, "assets", "bodies");
                Directory.CreateDirectory(_outputDirectory);
                StartCoroutine(RunExportQueue());
            }

            private IEnumerator RunExportQueue()
            {
                yield return null;

                if (_started)
                {
                    yield break;
                }

                _started = true;

                CelestialBody rootBody = HeliocentricPlanetFilter.FindRootBody();
                List<CelestialBody> planets = HeliocentricPlanetFilter.CollectHeliocentricPlanets(rootBody);

                if (planets.Count == 0)
                {
                    UnityEngine.Debug.LogWarning(LogPrefix + " body texture export: no heliocentric planets found.");
                    yield break;
                }

                System.Diagnostics.Stopwatch totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

                for (int i = 0; i < planets.Count; i++)
                {
                    CelestialBody body = planets[i];
                    _registry.EnsurePending(body.bodyName);
                    ExportBody(body);
                    yield return null;
                }

                List<CelestialBody> moons = HeliocentricMoonFilter.CollectHeliocentricMoons(rootBody);

                for (int i = 0; i < moons.Count; i++)
                {
                    CelestialBody body = moons[i];
                    _registry.EnsurePending(body.bodyName);
                    ExportBody(body);
                    yield return null;
                }

                totalStopwatch.Stop();

                if (totalStopwatch.Elapsed.TotalSeconds > ExportBudgetWarningSeconds)
                {
                    UnityEngine.Debug.LogWarning(string.Format(
                        "{0} body texture export finished in {1:F2}s (budget {2:F0}s, {3} planets, {4} moons).",
                        LogPrefix,
                        totalStopwatch.Elapsed.TotalSeconds,
                        ExportBudgetWarningSeconds,
                        planets.Count,
                        moons.Count));
                }
                else
                {
                    UnityEngine.Debug.Log(string.Format(
                        "{0} body texture export finished in {1:F2}s for {2} planets and {3} moons.",
                        LogPrefix,
                        totalStopwatch.Elapsed.TotalSeconds,
                        planets.Count,
                        moons.Count));
                }
            }

            private void ExportBody(CelestialBody body)
            {
                string bodyName = body.bodyName;
                string fileStem;
                BodyTextureExportState state = new BodyTextureExportState
                {
                    BodyName = bodyName,
                    Status = BodyTextureExportState.StatusPending
                };
                _registry.SetState(state);

                if (!BodyTextureFileNames.TrySanitize(bodyName, out fileStem))
                {
                    state.Status = BodyTextureExportState.StatusFailed;
                    state.LastError = "Unsafe body name.";
                    _registry.SetState(state);
                    UnityEngine.Debug.LogWarning(LogPrefix + " body texture " + bodyName + ": failed (unsafe name).");
                    return;
                }

                GameObject scaledBody = body.scaledBody;
                Material material = null;

                if (scaledBody != null)
                {
                    ScaledBodyMaterialResolver.TryResolve(scaledBody, out material);
                }

                string fingerprint = BodyTextureFingerprint.Compute(material);
                state.MaterialFingerprint = fingerprint;

                ScaledBodyTextureExportResult cached;

                if (ScaledBodyTextureExporter.TryLoadCachedExport(
                    bodyName,
                    _outputDirectory,
                    fingerprint,
                    out cached))
                {
                    ApplyReadyState(state, fileStem, cached);
                    _registry.SetState(state);
                    UnityEngine.Debug.Log(string.Format(
                        "{0} body texture {1}: ready (cached, {2}ms).",
                        LogPrefix,
                        bodyName,
                        0));
                    return;
                }

                ScaledBodyTextureExportResult exported = ScaledBodyTextureExporter.TryExport(body, _outputDirectory);
                state.Status = exported.Status;
                state.LastError = exported.ErrorMessage;
                state.MaterialFingerprint = exported.MaterialFingerprint;

                if (exported.Success)
                {
                    ApplyReadyState(state, fileStem, exported);
                }
                else if (exported.Status == BodyTextureExportState.StatusFailed && !state.RetryAttempted)
                {
                    state.RetryAttempted = true;
                    exported = ScaledBodyTextureExporter.TryExport(body, _outputDirectory);
                    state.Status = exported.Status;
                    state.LastError = exported.ErrorMessage;

                    if (exported.Success)
                    {
                        ApplyReadyState(state, fileStem, exported);
                    }
                }

                _registry.SetState(state);

                if (state.Status == BodyTextureExportState.StatusReady)
                {
                    UnityEngine.Debug.Log(string.Format(
                        "{0} body texture {1}: ready ({2}ms).",
                        LogPrefix,
                        bodyName,
                        exported != null ? exported.ElapsedMilliseconds : 0));
                }
                else if (state.Status == BodyTextureExportState.StatusUnsupported)
                {
                    UnityEngine.Debug.Log(string.Format(
                        "{0} body texture {1}: unsupported ({2}).",
                        LogPrefix,
                        bodyName,
                        state.LastError ?? "no albedo"));
                }
                else
                {
                    UnityEngine.Debug.LogWarning(string.Format(
                        "{0} body texture {1}: {2} ({3}).",
                        LogPrefix,
                        bodyName,
                        state.Status,
                        state.LastError ?? "unknown"));
                }
            }

            private static void ApplyReadyState(
                BodyTextureExportState state,
                string fileStem,
                ScaledBodyTextureExportResult exportResult)
            {
                state.Status = BodyTextureExportState.StatusReady;
                state.Revision = exportResult.Revision;
                state.RelativePath = exportResult.RelativePath;
                state.Url = BodyTextureFileNames.BuildTextureUrl(fileStem, exportResult.Revision);
            }
        }
    }
}
