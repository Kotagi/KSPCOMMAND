using System;
using System.Text;
using UnityEngine;

namespace KspWebMap
{
    public static class BodyTextureFingerprint
    {
        private static readonly string[] TexturePropertyNames =
        {
            "_MainTex",
            "_ColorMap",
            "_DetailCloudPatternTexture",
            "_MainTex_Xn",
            "_MainTex_Xp",
            "_MainTex_Yn",
            "_MainTex_Yp",
            "_MainTex_Zn",
            "_MainTex_Zp",
        };

        /// <summary>
        /// Session-stable source identity for disk cache (paths and texture names, not Unity instance IDs).
        /// </summary>
        public static string ComputeStable(GameObject scaledBody, Material material, string bodyName)
        {
            string onDemandFingerprint;
            if (KopernicusOnDemandTextureLoader.TryGetStableSourceFingerprint(scaledBody, out onDemandFingerprint))
            {
                return onDemandFingerprint;
            }

            return ComputeMaterialStable(material, bodyName);
        }

        public static bool FingerprintsMatch(string expected, string stored)
        {
            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(stored))
            {
                return false;
            }

            if (string.Equals(expected, stored, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        private static string ComputeMaterialStable(Material material, string bodyName)
        {
            if (material == null)
            {
                return string.Empty;
            }

            Shader shader = material.shader;
            string shaderName = shader != null ? shader.name : "null";

            if (IsScaledMesh2Shader(shaderName))
            {
                return "scaledmesh2|shader=" + shaderName + "|body=" + (bodyName ?? string.Empty);
            }

            StringBuilder builder = new StringBuilder(128);
            builder.Append("mat|shader=").Append(shaderName);

            for (int i = 0; i < TexturePropertyNames.Length; i++)
            {
                string propertyName = TexturePropertyNames[i];

                if (!material.HasProperty(propertyName))
                {
                    continue;
                }

                Texture texture = material.GetTexture(propertyName);

                builder.Append('|');
                builder.Append(propertyName);
                builder.Append('=');
                builder.Append(texture != null ? StableTextureToken(texture) : "0");
            }

            return builder.ToString();
        }

        private static bool IsScaledMesh2Shader(string shaderName)
        {
            return !string.IsNullOrEmpty(shaderName)
                && shaderName.IndexOf("ScaledMesh2", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string StableTextureToken(Texture texture)
        {
            string name = texture.name;
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return "anon:" + texture.width + "x" + texture.height;
        }
    }
}
