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

        public static string Compute(Material material)
        {
            if (material == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(128);
            Shader shader = material.shader;
            builder.Append(shader != null ? shader.name : "null");

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
                builder.Append(texture != null ? texture.GetInstanceID().ToString() : "0");
            }

            return builder.ToString();
        }
    }
}
