using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KspWebMap
{
    /// <summary>
    /// Diagnostic probes for discovering how modded ScaledSpace materials expose albedo.
    /// Used by BodyTextureAdaptationProbeService to adapt RSS / planet-pack texture export.
    /// </summary>
    public static class BodyTextureAlbedoStrategies
    {
        private static readonly string[] StockMainProperties = { "_MainTex", "_ColorMap" };
        private static readonly string[] StockDetailProperties = { "_DetailCloudPatternTexture" };

        private static readonly string[] SkipPropertySubstrings =
        {
            "bump", "normal", "spec", "gloss", "height", "parallax", "occlusion",
            "emiss", "mask", "depth", "rough", "metallic", "noise", "ramp"
        };

        private static readonly string[] PreferPropertySubstrings =
        {
            "maintex", "colormap", "albedo", "diffuse", "base", "color", "detailcloud"
        };

        public sealed class RendererProbe
        {
            public int Index;
            public string Path;
            public string ShaderName;
            public bool HasMaterial;
            public string MainTextureName;
            public int MainTextureWidth;
            public int MainTextureHeight;
            public List<TexturePropertyProbe> TextureProperties = new List<TexturePropertyProbe>();
        }

        public sealed class TexturePropertyProbe
        {
            public string Name;
            public bool Assigned;
            public int Width;
            public int Height;
        }

        public sealed class StrategyAttempt
        {
            public string StrategyId;
            public int RendererIndex;
            public string RendererPath;
            public string ShaderName;
            public string PropertyName;
            public bool TextureResolved;
            public bool CaptureSucceeded;
            public int CapturedWidth;
            public int CapturedHeight;
            public string PreviewPath;
            public string Error;
        }

        public sealed class BodyProbeReport
        {
            public string BodyName;
            public float UniversalTimeSeconds;
            public int PassIndex;
            public bool ScaledBodyPresent;
            public int RendererCount;
            public List<RendererProbe> Renderers = new List<RendererProbe>();
            public List<StrategyAttempt> Attempts = new List<StrategyAttempt>();
            public StrategyAttempt BestAttempt;
        }

        public static BodyProbeReport ProbeBody(
            CelestialBody body,
            int passIndex,
            float universalTimeSeconds,
            string previewOutputDirectory)
        {
            BodyProbeReport report = new BodyProbeReport
            {
                BodyName = body != null ? body.bodyName : "(null)",
                PassIndex = passIndex,
                UniversalTimeSeconds = universalTimeSeconds
            };

            if (body == null)
            {
                return report;
            }

            GameObject scaledBody = body.scaledBody;
            report.ScaledBodyPresent = scaledBody != null;

            if (scaledBody == null)
            {
                return report;
            }

            MeshRenderer[] renderers = scaledBody.GetComponentsInChildren<MeshRenderer>(true);
            report.RendererCount = renderers != null ? renderers.Length : 0;

            if (renderers == null || renderers.Length == 0)
            {
                return report;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                RendererProbe rendererProbe = BuildRendererProbe(renderer, i);
                report.Renderers.Add(rendererProbe);
                RunStrategiesForRenderer(body.bodyName, renderer, rendererProbe, previewOutputDirectory, report);
            }

            report.BestAttempt = SelectBestAttempt(report.Attempts);
            return report;
        }

        public static string FormatReport(BodyProbeReport report)
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append("body=").Append(report.BodyName);
            builder.Append(" pass=").Append(report.PassIndex);
            builder.Append(" ut=").Append(report.UniversalTimeSeconds.ToString("F1"));
            builder.Append(" scaledBody=").Append(report.ScaledBodyPresent);
            builder.Append(" renderers=").Append(report.RendererCount);

            for (int i = 0; i < report.Renderers.Count; i++)
            {
                RendererProbe renderer = report.Renderers[i];
                builder.Append("\n  [R").Append(renderer.Index).Append("] ")
                    .Append(renderer.Path)
                    .Append(" shader=").Append(renderer.ShaderName ?? "null");

                if (!string.IsNullOrEmpty(renderer.MainTextureName))
                {
                    builder.Append(" mainTexture=")
                        .Append(renderer.MainTextureName)
                        .Append(" ")
                        .Append(renderer.MainTextureWidth)
                        .Append("x")
                        .Append(renderer.MainTextureHeight);
                }

                for (int p = 0; p < renderer.TextureProperties.Count; p++)
                {
                    TexturePropertyProbe prop = renderer.TextureProperties[p];
                    builder.Append("\n    tex.")
                        .Append(prop.Name)
                        .Append(" assigned=")
                        .Append(prop.Assigned);
                    if (prop.Assigned)
                    {
                        builder.Append(" ").Append(prop.Width).Append("x").Append(prop.Height);
                    }
                }
            }

            for (int i = 0; i < report.Attempts.Count; i++)
            {
                StrategyAttempt attempt = report.Attempts[i];
                if (!attempt.TextureResolved && !attempt.CaptureSucceeded)
                {
                    continue;
                }

                builder.Append("\n  try ")
                    .Append(attempt.StrategyId)
                    .Append(" R").Append(attempt.RendererIndex);
                if (!string.IsNullOrEmpty(attempt.PropertyName))
                {
                    builder.Append(" prop=").Append(attempt.PropertyName);
                }

                builder.Append(" resolved=").Append(attempt.TextureResolved);
                builder.Append(" capture=").Append(attempt.CaptureSucceeded);
                if (attempt.CaptureSucceeded)
                {
                    builder.Append(" size=")
                        .Append(attempt.CapturedWidth)
                        .Append("x")
                        .Append(attempt.CapturedHeight);
                    if (!string.IsNullOrEmpty(attempt.PreviewPath))
                    {
                        builder.Append(" preview=").Append(attempt.PreviewPath);
                    }
                }

                if (!string.IsNullOrEmpty(attempt.Error))
                {
                    builder.Append(" err=").Append(attempt.Error);
                }
            }

            if (report.BestAttempt != null && report.BestAttempt.CaptureSucceeded)
            {
                builder.Append("\n  BEST ")
                    .Append(report.BestAttempt.StrategyId)
                    .Append(" R").Append(report.BestAttempt.RendererIndex);
                if (!string.IsNullOrEmpty(report.BestAttempt.PropertyName))
                {
                    builder.Append(" prop=").Append(report.BestAttempt.PropertyName);
                }
            }
            else
            {
                builder.Append("\n  BEST none");
            }

            return builder.ToString();
        }

        private static RendererProbe BuildRendererProbe(MeshRenderer renderer, int index)
        {
            Material material = renderer.sharedMaterial;
            RendererProbe probe = new RendererProbe
            {
                Index = index,
                Path = BuildTransformPath(renderer.transform),
                HasMaterial = material != null,
                ShaderName = material != null && material.shader != null ? material.shader.name : null
            };

            if (material == null)
            {
                return probe;
            }

            Texture main = material.mainTexture;
            if (main != null)
            {
                probe.MainTextureName = main.name;
                probe.MainTextureWidth = main.width;
                probe.MainTextureHeight = main.height;
            }

            string[] propertyNames = material.GetTexturePropertyNames();
            for (int i = 0; i < propertyNames.Length; i++)
            {
                string propertyName = propertyNames[i];
                Texture texture = material.GetTexture(propertyName);
                TexturePropertyProbe propertyProbe = new TexturePropertyProbe
                {
                    Name = propertyName,
                    Assigned = texture != null
                };

                if (texture != null)
                {
                    propertyProbe.Width = texture.width;
                    propertyProbe.Height = texture.height;
                }

                probe.TextureProperties.Add(propertyProbe);
            }

            return probe;
        }

        private static void RunStrategiesForRenderer(
            string bodyName,
            MeshRenderer renderer,
            RendererProbe rendererProbe,
            string previewOutputDirectory,
            BodyProbeReport report)
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            TryStockComposite(bodyName, renderer, rendererProbe, previewOutputDirectory, report);
            TryPropertyList(bodyName, renderer, rendererProbe, previewOutputDirectory, report, StockMainProperties, "stock-main");
            TryPropertyList(bodyName, renderer, rendererProbe, previewOutputDirectory, report, StockDetailProperties, "stock-detail");

            Texture mainTexture = material.mainTexture;
            if (mainTexture != null)
            {
                TryCapture(bodyName, renderer, rendererProbe, previewOutputDirectory, report,
                    "unity-mainTexture", null, mainTexture);
            }

            List<string> scanned = new List<string>();
            for (int i = 0; i < rendererProbe.TextureProperties.Count; i++)
            {
                TexturePropertyProbe property = rendererProbe.TextureProperties[i];
                if (!property.Assigned || ShouldSkipProperty(property.Name))
                {
                    continue;
                }

                scanned.Add(property.Name);
            }

            scanned.Sort((a, b) => ComparePropertyPreference(a, b));

            for (int i = 0; i < scanned.Count; i++)
            {
                string propertyName = scanned[i];
                Texture texture = material.GetTexture(propertyName);
                TryCapture(bodyName, renderer, rendererProbe, previewOutputDirectory, report,
                    "scan-" + propertyName, propertyName, texture);
            }
        }

        private static void TryStockComposite(
            string bodyName,
            MeshRenderer renderer,
            RendererProbe rendererProbe,
            string previewOutputDirectory,
            BodyProbeReport report)
        {
            Material material = renderer.sharedMaterial;
            Texture2D readable = CaptureStockComposite(material);
            StrategyAttempt attempt = new StrategyAttempt
            {
                StrategyId = "stock-composite",
                RendererIndex = rendererProbe.Index,
                RendererPath = rendererProbe.Path,
                ShaderName = rendererProbe.ShaderName,
                PropertyName = "_MainTex+_DetailCloudPatternTexture",
                TextureResolved = readable != null
            };

            if (readable == null)
            {
                attempt.Error = "stock composite returned null";
                report.Attempts.Add(attempt);
                return;
            }

            FinalizeCapture(bodyName, attempt, readable, previewOutputDirectory, report);
        }

        private static void TryPropertyList(
            string bodyName,
            MeshRenderer renderer,
            RendererProbe rendererProbe,
            string previewOutputDirectory,
            BodyProbeReport report,
            string[] propertyNames,
            string strategyPrefix)
        {
            Material material = renderer.sharedMaterial;

            for (int i = 0; i < propertyNames.Length; i++)
            {
                string propertyName = propertyNames[i];
                if (!material.HasProperty(propertyName))
                {
                    continue;
                }

                Texture texture = material.GetTexture(propertyName);
                TryCapture(bodyName, renderer, rendererProbe, previewOutputDirectory, report,
                    strategyPrefix + "-" + propertyName.TrimStart('_'),
                    propertyName,
                    texture);
            }
        }

        private static void TryCapture(
            string bodyName,
            MeshRenderer renderer,
            RendererProbe rendererProbe,
            string previewOutputDirectory,
            BodyProbeReport report,
            string strategyId,
            string propertyName,
            Texture texture)
        {
            StrategyAttempt attempt = new StrategyAttempt
            {
                StrategyId = strategyId,
                RendererIndex = rendererProbe.Index,
                RendererPath = rendererProbe.Path,
                ShaderName = rendererProbe.ShaderName,
                PropertyName = propertyName,
                TextureResolved = texture != null
            };

            if (texture == null)
            {
                attempt.Error = "texture null";
                report.Attempts.Add(attempt);
                return;
            }

            Texture2D readable = CaptureTexture(texture);
            if (readable == null)
            {
                attempt.Error = "blit/read failed";
                report.Attempts.Add(attempt);
                return;
            }

            FinalizeCapture(bodyName, attempt, readable, previewOutputDirectory, report);
        }

        private static void FinalizeCapture(
            string bodyName,
            StrategyAttempt attempt,
            Texture2D readable,
            string previewOutputDirectory,
            BodyProbeReport report)
        {
            try
            {
                attempt.CapturedWidth = readable.width;
                attempt.CapturedHeight = readable.height;

                string fileStem;
                if (!BodyTextureFileNames.TrySanitize(bodyName, out fileStem))
                {
                    attempt.Error = "unsafe body name";
                    attempt.CaptureSucceeded = false;
                    report.Attempts.Add(attempt);
                    UnityEngine.Object.Destroy(readable);
                    return;
                }

                string safeStrategy = SanitizeStrategyId(attempt.StrategyId);
                string fileName = fileStem + "__" + safeStrategy + "__R" + attempt.RendererIndex + ".jpg";
                string path = System.IO.Path.Combine(previewOutputDirectory, fileName);
                byte[] jpegBytes = ImageConversion.EncodeToJPG(readable, 85);
                if (jpegBytes == null || jpegBytes.Length == 0)
                {
                    attempt.Error = "jpeg encode empty";
                    attempt.CaptureSucceeded = false;
                }
                else
                {
                    System.IO.File.WriteAllBytes(path, jpegBytes);
                    attempt.PreviewPath = path;
                    attempt.CaptureSucceeded = true;
                }
            }
            catch (Exception ex)
            {
                attempt.Error = ex.Message;
                attempt.CaptureSucceeded = false;
            }
            finally
            {
                UnityEngine.Object.Destroy(readable);
            }

            report.Attempts.Add(attempt);
        }

        private static StrategyAttempt SelectBestAttempt(List<StrategyAttempt> attempts)
        {
            StrategyAttempt best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < attempts.Count; i++)
            {
                StrategyAttempt attempt = attempts[i];
                if (!attempt.CaptureSucceeded)
                {
                    continue;
                }

                int score = attempt.CapturedWidth * attempt.CapturedHeight;
                score += PropertyPreferenceScore(attempt.PropertyName) * 1000;
                if (attempt.StrategyId.StartsWith("stock", StringComparison.Ordinal))
                {
                    score += 500;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = attempt;
                }
            }

            return best;
        }

        private static Texture2D CaptureStockComposite(Material material)
        {
            Texture mainTexture = ResolveStockMain(material);
            Texture detailTexture = ResolveStockDetail(material);

            if (mainTexture == null && detailTexture == null)
            {
                return null;
            }

            if (detailTexture == null)
            {
                return CaptureTexture(mainTexture);
            }

            if (mainTexture == null)
            {
                return CaptureTexture(detailTexture);
            }

            Texture2D mainCaptured = CaptureTexture(mainTexture);
            Texture2D detailCaptured = CaptureTexture(detailTexture);

            if (mainCaptured == null)
            {
                UnityEngine.Object.Destroy(detailCaptured);
                return null;
            }

            if (detailCaptured == null)
            {
                return mainCaptured;
            }

            int width = Mathf.Max(mainCaptured.width, detailCaptured.width);
            int height = Mathf.Max(mainCaptured.height, detailCaptured.height);
            Texture2D composite = new Texture2D(width, height, TextureFormat.RGB24, false);
            Color[] basePixels = mainCaptured.GetPixels();
            Color[] detailPixels = detailCaptured.GetPixels();
            Color[] output = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int mainX = x * mainCaptured.width / width;
                    int mainY = y * mainCaptured.height / height;
                    int detailX = x * detailCaptured.width / width;
                    int detailY = y * detailCaptured.height / height;
                    Color baseColor = basePixels[mainY * mainCaptured.width + mainX];
                    Color detailColor = detailPixels[detailY * detailCaptured.width + detailX];
                    float alpha = detailColor.a;
                    output[y * width + x] = Color.Lerp(baseColor, detailColor, alpha);
                }
            }

            composite.SetPixels(output);
            composite.Apply();
            UnityEngine.Object.Destroy(mainCaptured);
            UnityEngine.Object.Destroy(detailCaptured);
            return composite;
        }

        private static Texture ResolveStockMain(Material material)
        {
            if (material.HasProperty("_MainTex"))
            {
                Texture main = material.GetTexture("_MainTex");
                if (main != null)
                {
                    return main;
                }
            }

            if (material.HasProperty("_ColorMap"))
            {
                return material.GetTexture("_ColorMap");
            }

            return null;
        }

        private static Texture ResolveStockDetail(Material material)
        {
            if (!material.HasProperty("_DetailCloudPatternTexture"))
            {
                return null;
            }

            return material.GetTexture("_DetailCloudPatternTexture");
        }

        private static Texture2D CaptureTexture(Texture source)
        {
            if (source == null)
            {
                return null;
            }

            RenderTexture temporary = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = temporary;

                Texture2D readable = new Texture2D(
                    temporary.width,
                    temporary.height,
                    TextureFormat.RGB24,
                    false);
                readable.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
                readable.Apply();
                RenderTexture.active = previous;
                return readable;
            }
            catch
            {
                return null;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static bool ShouldSkipProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return true;
            }

            string lower = propertyName.ToLowerInvariant();
            for (int i = 0; i < SkipPropertySubstrings.Length; i++)
            {
                if (lower.IndexOf(SkipPropertySubstrings[i], StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ComparePropertyPreference(string a, string b)
        {
            return PropertyPreferenceScore(b).CompareTo(PropertyPreferenceScore(a));
        }

        private static int PropertyPreferenceScore(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return 0;
            }

            string lower = propertyName.ToLowerInvariant();
            int score = 0;
            for (int i = 0; i < PreferPropertySubstrings.Length; i++)
            {
                if (lower.IndexOf(PreferPropertySubstrings[i], StringComparison.Ordinal) >= 0)
                {
                    score += 10 - i;
                }
            }

            return score;
        }

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "(null)";
            }

            StringBuilder builder = new StringBuilder(transform.name);
            Transform current = transform.parent;
            int depth = 0;

            while (current != null && depth < 6)
            {
                builder.Insert(0, current.name + "/");
                current = current.parent;
                depth++;
            }

            return builder.ToString();
        }

        private static string SanitizeStrategyId(string strategyId)
        {
            if (string.IsNullOrEmpty(strategyId))
            {
                return "unknown";
            }

            StringBuilder builder = new StringBuilder(strategyId.Length);
            for (int i = 0; i < strategyId.Length; i++)
            {
                char c = strategyId[i];
                if ((c >= 'a' && c <= 'z')
                    || (c >= 'A' && c <= 'Z')
                    || (c >= '0' && c <= '9')
                    || c == '_' || c == '-')
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }
    }
}
