using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace KspWebMap
{
    public sealed class ScaledBodyTextureExportResult
    {
        public bool Success;
        public string Status;
        public string Revision;
        public string RelativePath;
        public string MaterialFingerprint;
        public string ErrorMessage;
        public long ElapsedMilliseconds;
        public long FileSizeBytes;
    }

    public static class ScaledBodyTextureExporter
    {
        private const string LogPrefix = "[KspWebMap]";
        private const int MaxTextureEdgePixels = 1024;
        private const int JpegQuality = 85;
        private const long TargetFileSizeBytes = 400L * 1024L;

        public static ScaledBodyTextureExportResult TryExport(
            CelestialBody body,
            string outputDirectory)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            ScaledBodyTextureExportResult result = new ScaledBodyTextureExportResult();

            if (body == null)
            {
                result.Status = BodyTextureExportState.StatusFailed;
                result.ErrorMessage = "Body is null.";
                return result;
            }

            string fileStem;

            if (!BodyTextureFileNames.TrySanitize(body.bodyName, out fileStem))
            {
                result.Status = BodyTextureExportState.StatusFailed;
                result.ErrorMessage = "Body name is not a safe file stem.";
                return result;
            }

            GameObject scaledBody = body.scaledBody;

            if (scaledBody == null)
            {
                result.Status = BodyTextureExportState.StatusUnsupported;
                result.ErrorMessage = "scaledBody is null.";
                return result;
            }

            MeshRenderer renderer = scaledBody.GetComponentInChildren<MeshRenderer>(true);

            if (renderer == null || renderer.sharedMaterial == null)
            {
                result.Status = BodyTextureExportState.StatusUnsupported;
                result.ErrorMessage = "No scaledBody MeshRenderer/material.";
                return result;
            }

            Material material = renderer.sharedMaterial;
            result.MaterialFingerprint = BodyTextureFingerprint.Compute(material);

            Texture2D readable = null;
            Texture2D resized = null;

            try
            {
                readable = CaptureAlbedoTexture(material);

                if (readable == null)
                {
                    result.Status = BodyTextureExportState.StatusUnsupported;
                    result.ErrorMessage = "No albedo texture on scaled material.";
                    return result;
                }

                resized = DownscaleTexture(readable, MaxTextureEdgePixels);
                byte[] jpegBytes = ImageConversion.EncodeToJPG(resized, JpegQuality);

                if (jpegBytes == null || jpegBytes.Length == 0)
                {
                    result.Status = BodyTextureExportState.StatusFailed;
                    result.ErrorMessage = "JPEG encode returned empty bytes.";
                    return result;
                }

                Directory.CreateDirectory(outputDirectory);

                string jpegFileName = BodyTextureFileNames.BuildJpegFileName(fileStem);
                string metaFileName = BodyTextureFileNames.BuildMetaFileName(fileStem);
                string jpegPath = Path.Combine(outputDirectory, jpegFileName);
                string metaPath = Path.Combine(outputDirectory, metaFileName);

                File.WriteAllBytes(jpegPath, jpegBytes);
                File.WriteAllText(metaPath, result.MaterialFingerprint ?? string.Empty);

                result.Success = true;
                result.Status = BodyTextureExportState.StatusReady;
                result.Revision = ComputeRevision(jpegBytes);
                result.RelativePath = "assets/bodies/" + jpegFileName;
                result.FileSizeBytes = jpegBytes.Length;

                if (result.FileSizeBytes > TargetFileSizeBytes)
                {
                    UnityEngine.Debug.LogWarning(string.Format(
                        "{0} body texture {1}: {2} KB exceeds target ({3} KB).",
                        LogPrefix,
                        body.bodyName,
                        result.FileSizeBytes / 1024,
                        TargetFileSizeBytes / 1024));
                }
            }
            catch (Exception ex)
            {
                result.Status = BodyTextureExportState.StatusFailed;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                if (readable != null)
                {
                    UnityEngine.Object.Destroy(readable);
                }

                if (resized != null && resized != readable)
                {
                    UnityEngine.Object.Destroy(resized);
                }

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        public static bool TryLoadCachedExport(
            string bodyName,
            string outputDirectory,
            string expectedFingerprint,
            out ScaledBodyTextureExportResult result)
        {
            result = new ScaledBodyTextureExportResult();
            string fileStem;

            if (!BodyTextureFileNames.TrySanitize(bodyName, out fileStem))
            {
                return false;
            }

            string jpegPath = Path.Combine(
                outputDirectory,
                BodyTextureFileNames.BuildJpegFileName(fileStem));
            string metaPath = Path.Combine(
                outputDirectory,
                BodyTextureFileNames.BuildMetaFileName(fileStem));

            if (!File.Exists(jpegPath) || !File.Exists(metaPath))
            {
                return false;
            }

            string storedFingerprint;

            try
            {
                storedFingerprint = File.ReadAllText(metaPath).Trim();
            }
            catch
            {
                return false;
            }

            if (!string.Equals(storedFingerprint, expectedFingerprint, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                byte[] jpegBytes = File.ReadAllBytes(jpegPath);
                result.Success = true;
                result.Status = BodyTextureExportState.StatusReady;
                result.Revision = ComputeRevision(jpegBytes);
                result.RelativePath = "assets/bodies/" + BodyTextureFileNames.BuildJpegFileName(fileStem);
                result.MaterialFingerprint = storedFingerprint;
                result.FileSizeBytes = jpegBytes.Length;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Texture2D CaptureAlbedoTexture(Material material)
        {
            Texture mainTexture = ResolveMainAlbedo(material);
            Texture detailTexture = ResolveDetailTexture(material);

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

        private static Texture ResolveMainAlbedo(Material material)
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

        private static Texture ResolveDetailTexture(Material material)
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
            finally
            {
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static Texture2D DownscaleTexture(Texture2D source, int maxEdge)
        {
            int width = source.width;
            int height = source.height;
            int largestEdge = Mathf.Max(width, height);

            if (largestEdge <= maxEdge)
            {
                return source;
            }

            float scale = maxEdge / (float)largestEdge;
            int targetWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
            int targetHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));

            RenderTexture temporary = RenderTexture.GetTemporary(
                targetWidth,
                targetHeight,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = temporary;

                Texture2D resized = new Texture2D(
                    targetWidth,
                    targetHeight,
                    TextureFormat.RGB24,
                    false);
                resized.ReadPixels(new Rect(0f, 0f, targetWidth, targetHeight), 0, 0);
                resized.Apply();
                RenderTexture.active = previous;

                UnityEngine.Object.Destroy(source);
                return resized;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static string ComputeRevision(byte[] fileBytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(fileBytes);
                StringBuilder builder = new StringBuilder(8);

                for (int i = 0; i < 4; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
