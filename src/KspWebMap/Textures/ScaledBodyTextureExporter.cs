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

            Material material;
            if (!ScaledBodyMaterialResolver.TryResolve(scaledBody, out material))
            {
                result.Status = BodyTextureExportState.StatusUnsupported;
                result.ErrorMessage = "No scaledBody MeshRenderer/material.";
                return result;
            }

            result.MaterialFingerprint = BodyTextureFingerprint.Compute(material);

            Texture2D readable = null;
            Texture2D resized = null;
            Texture2D exportReady = null;

            try
            {
                bool fromCubemapEquirect;
                Material fingerprintMaterial;
                readable = CaptureAlbedoTexture(
                    scaledBody,
                    body.bodyName,
                    material,
                    out fromCubemapEquirect,
                    out fingerprintMaterial);

                if (fingerprintMaterial != null)
                {
                    result.MaterialFingerprint = BodyTextureFingerprint.Compute(fingerprintMaterial);
                }

                if (readable == null)
                {
                    result.Status = BodyTextureExportState.StatusUnsupported;
                    result.ErrorMessage = "No albedo texture on scaled material.";
                    return result;
                }

                resized = DownscaleTexture(readable, MaxTextureEdgePixels);
                if (resized != readable)
                {
                    UnityEngine.Object.Destroy(readable);
                    readable = null;
                }
                else
                {
                    resized = readable;
                    readable = null;
                }

                // Flat _MainTex exports need flip-X for SphereGeometry longitude; cubemap equirect does not.
                if (fromCubemapEquirect)
                {
                    exportReady = resized;
                }
                else
                {
                    exportReady = FlipTextureHorizontal(resized);
                    if (exportReady != resized && resized != null)
                    {
                        UnityEngine.Object.Destroy(resized);
                        resized = null;
                    }
                }

                byte[] jpegBytes = ImageConversion.EncodeToJPG(exportReady, JpegQuality);

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
                File.WriteAllText(
                    metaPath,
                    BodyTextureExportLayout.BuildMetaFileContent(result.MaterialFingerprint));

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
                if (exportReady != null)
                {
                    UnityEngine.Object.Destroy(exportReady);
                }
                else if (resized != null)
                {
                    UnityEngine.Object.Destroy(resized);
                }
                else if (readable != null)
                {
                    UnityEngine.Object.Destroy(readable);
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

            string layoutId;

            try
            {
                string metaText = File.ReadAllText(metaPath);
                if (!BodyTextureExportLayout.TryParseMetaFileContent(metaText, out storedFingerprint, out layoutId))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            if (!string.Equals(storedFingerprint, expectedFingerprint, StringComparison.Ordinal))
            {
                return false;
            }

            if (!BodyTextureExportLayout.IsLayoutCurrent(layoutId))
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

        private static Texture2D CaptureAlbedoTexture(
            GameObject scaledBody,
            string bodyName,
            Material material,
            out bool fromCubemapEquirect,
            out Material fingerprintMaterial)
        {
            fromCubemapEquirect = false;
            fingerprintMaterial = null;

            Texture2D captured = CaptureAlbedoFromMaterial(material, out fromCubemapEquirect);
            if (captured != null)
            {
                return captured;
            }

            if (scaledBody == null || !KopernicusOnDemandTextureLoader.HasOnDemandComponent(scaledBody))
            {
                return null;
            }

            Material loadedMaterial;
            bool onDemandFromCubemap = false;
            if (!KopernicusOnDemandTextureLoader.TryCaptureAlbedo(
                    scaledBody,
                    bodyName,
                    mat => CaptureAlbedoFromMaterial(mat, out onDemandFromCubemap),
                    out captured,
                    out loadedMaterial))
            {
                return null;
            }

            fromCubemapEquirect = onDemandFromCubemap;

            if (loadedMaterial != null)
            {
                fingerprintMaterial = loadedMaterial;
            }

            return captured;
        }

        private static Texture2D CaptureAlbedoFromMaterial(Material material, out bool fromCubemapEquirect)
        {
            fromCubemapEquirect = false;

            if (material == null)
            {
                return null;
            }

            if (ScaledMesh2CubemapExporter.HasCompleteCubemapFaces(material))
            {
                Texture2D equirect = ScaledMesh2CubemapExporter.TryCaptureEquirectangular(material);
                if (equirect != null)
                {
                    fromCubemapEquirect = true;
                    return equirect;
                }
            }

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
                Texture colorMap = material.GetTexture("_ColorMap");
                if (colorMap != null)
                {
                    return colorMap;
                }
            }

            return material.mainTexture;
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

        /// <summary>
        /// Longitude handedness for generic SphereGeometry UVs (decoupled from mesh spin).
        /// </summary>
        private static Texture2D FlipTextureHorizontal(Texture2D source)
        {
            if (source == null)
            {
                return null;
            }

            int width = source.width;
            int height = source.height;
            Texture2D flipped = new Texture2D(width, height, source.format, false);
            Color[] pixels = source.GetPixels();
            Color[] output = new Color[pixels.Length];

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    output[row + x] = pixels[row + (width - 1 - x)];
                }
            }

            flipped.SetPixels(output);
            flipped.Apply();
            return flipped;
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
