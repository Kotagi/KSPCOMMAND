using System;
using UnityEngine;

namespace KspWebMap
{
    public sealed class ScaledMesh2CubemapFaces
    {
        public CubemapFacePixels Xn;
        public CubemapFacePixels Xp;
        public CubemapFacePixels Yn;
        public CubemapFacePixels Yp;
        public CubemapFacePixels Zn;
        public CubemapFacePixels Zp;

        public bool IsComplete
        {
            get
            {
                return IsReady(Xn) && IsReady(Xp) && IsReady(Yn)
                    && IsReady(Yp) && IsReady(Zn) && IsReady(Zp);
            }
        }

        public void Destroy()
        {
            Xn = null;
            Xp = null;
            Yn = null;
            Yp = null;
            Zn = null;
            Zp = null;
        }

        private static bool IsReady(CubemapFacePixels face)
        {
            return face != null && face.Pixels != null && face.Pixels.Length > 0;
        }
    }

    public sealed class CubemapFacePixels
    {
        public Color[] Pixels;
        public int Width;
        public int Height;
    }

    /// <summary>
    /// RSS/Kopernicus Terrain/ScaledMesh2 stores albedo as six cubemap face textures.
    /// Stitch them into an equirectangular map for the web sphere UV layout.
    /// </summary>
    public static class ScaledMesh2CubemapExporter
    {
        private const int MaxFaceCaptureEdge = 256;
        private const int EquirectWidth = 512;
        private const int EquirectHeight = 256;

        private static readonly string[] FacePropertyNames =
        {
            "_MainTex_Xn",
            "_MainTex_Xp",
            "_MainTex_Yn",
            "_MainTex_Yp",
            "_MainTex_Zn",
            "_MainTex_Zp",
        };

        public static bool HasCompleteCubemapFaces(Material material)
        {
            if (material == null)
            {
                return false;
            }

            for (int i = 0; i < FacePropertyNames.Length; i++)
            {
                string propertyName = FacePropertyNames[i];
                if (!material.HasProperty(propertyName) || material.GetTexture(propertyName) == null)
                {
                    return false;
                }
            }

            return true;
        }

        public static Texture2D TryCaptureEquirectangular(Material material)
        {
            if (!HasCompleteCubemapFaces(material))
            {
                return null;
            }

            ScaledMesh2CubemapFaces faces = CaptureFaces(material);
            if (!faces.IsComplete)
            {
                faces.Destroy();
                return null;
            }

            try
            {
                return BuildEquirectangular(faces, EquirectWidth, EquirectHeight);
            }
            finally
            {
                faces.Destroy();
            }
        }

        private static ScaledMesh2CubemapFaces CaptureFaces(Material material)
        {
            ScaledMesh2CubemapFaces faces = new ScaledMesh2CubemapFaces();
            faces.Xn = CaptureFacePixels(material, "_MainTex_Xn");
            faces.Xp = CaptureFacePixels(material, "_MainTex_Xp");
            faces.Yn = CaptureFacePixels(material, "_MainTex_Yn");
            faces.Yp = CaptureFacePixels(material, "_MainTex_Yp");
            faces.Zn = CaptureFacePixels(material, "_MainTex_Zn");
            faces.Zp = CaptureFacePixels(material, "_MainTex_Zp");
            return faces;
        }

        private static CubemapFacePixels CaptureFacePixels(Material material, string propertyName)
        {
            Texture source = material.GetTexture(propertyName);
            if (source == null)
            {
                return null;
            }

            int targetEdge = Mathf.Min(MaxFaceCaptureEdge, Mathf.Max(source.width, source.height));
            if (targetEdge < 1)
            {
                targetEdge = MaxFaceCaptureEdge;
            }

            RenderTexture temporary = RenderTexture.GetTemporary(
                targetEdge,
                targetEdge,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            Texture2D readable = null;

            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = temporary;

                readable = new Texture2D(targetEdge, targetEdge, TextureFormat.RGB24, false);
                readable.ReadPixels(new Rect(0f, 0f, targetEdge, targetEdge), 0, 0);
                readable.Apply();
                RenderTexture.active = previous;

                CubemapFacePixels face = new CubemapFacePixels
                {
                    Pixels = readable.GetPixels(),
                    Width = readable.width,
                    Height = readable.height,
                };
                UnityEngine.Object.Destroy(readable);
                return face;
            }
            catch
            {
                if (readable != null)
                {
                    UnityEngine.Object.Destroy(readable);
                }

                return null;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static Texture2D BuildEquirectangular(
            ScaledMesh2CubemapFaces faces,
            int width,
            int height)
        {
            Texture2D output = new Texture2D(width, height, TextureFormat.RGB24, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                // Unity row y=0 is texture bottom → JPEG bottom; Three.js flipY maps JPEG top to uv.y=1 (+Y pole).
                float lat = (v - 0.5f) * Mathf.PI;

                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float lon = (u - 0.5f) * 2f * Mathf.PI;
                    float cosLat = Mathf.Cos(lat);
                    Vector3 direction = new Vector3(
                        cosLat * Mathf.Cos(lon),
                        Mathf.Sin(lat),
                        cosLat * Mathf.Sin(lon));

                    pixels[y * width + x] = SampleCubemap(faces, direction);
                }
            }

            output.SetPixels(pixels);
            output.Apply();
            return output;
        }

        private static Color SampleCubemap(ScaledMesh2CubemapFaces faces, Vector3 direction)
        {
            direction.Normalize();

            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);
            float absZ = Mathf.Abs(direction.z);

            CubemapFacePixels face;
            float uc;
            float vc;

            if (absX >= absY && absX >= absZ)
            {
                if (direction.x >= 0f)
                {
                    face = faces.Xp;
                    uc = -direction.z / direction.x;
                    vc = -direction.y / direction.x;
                }
                else
                {
                    face = faces.Xn;
                    uc = direction.z / -direction.x;
                    vc = -direction.y / -direction.x;
                }
            }
            else if (absY >= absX && absY >= absZ)
            {
                if (direction.y >= 0f)
                {
                    face = faces.Yp;
                    uc = direction.x / direction.y;
                    vc = direction.z / direction.y;
                }
                else
                {
                    face = faces.Yn;
                    uc = direction.x / -direction.y;
                    vc = -direction.z / -direction.y;
                }
            }
            else if (direction.z >= 0f)
            {
                face = faces.Zp;
                uc = direction.x / direction.z;
                vc = -direction.y / direction.z;
            }
            else
            {
                face = faces.Zn;
                uc = -direction.x / -direction.z;
                vc = -direction.y / -direction.z;
            }

            float sampleU = uc * 0.5f + 0.5f;
            float sampleV = vc * 0.5f + 0.5f;
            return SampleBilinear(face, sampleU, sampleV);
        }

        private static Color SampleBilinear(CubemapFacePixels face, float u, float v)
        {
            if (face == null || face.Pixels == null || face.Width < 1 || face.Height < 1)
            {
                return Color.black;
            }

            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            int width = face.Width;
            int height = face.Height;
            float x = u * (width - 1);
            float y = v * (height - 1);

            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            float tx = x - x0;
            float ty = y - y0;

            Color c00 = face.Pixels[y0 * width + x0];
            Color c10 = face.Pixels[y0 * width + x1];
            Color c01 = face.Pixels[y1 * width + x0];
            Color c11 = face.Pixels[y1 * width + x1];

            Color cx0 = Color.Lerp(c00, c10, tx);
            Color cx1 = Color.Lerp(c01, c11, tx);
            return Color.Lerp(cx0, cx1, ty);
        }
    }
}
