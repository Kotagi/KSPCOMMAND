using System;
using System.Text;

namespace KspWebMap
{
    public static class BodyTextureFileNames
    {
        public static bool TrySanitize(string bodyName, out string fileStem)
        {
            fileStem = null;

            if (string.IsNullOrEmpty(bodyName))
            {
                return false;
            }

            if (bodyName.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            StringBuilder builder = new StringBuilder(bodyName.Length);

            for (int i = 0; i < bodyName.Length; i++)
            {
                char c = bodyName[i];

                if ((c >= 'a' && c <= 'z')
                    || (c >= 'A' && c <= 'Z')
                    || (c >= '0' && c <= '9')
                    || c == '_'
                    || c == '-')
                {
                    builder.Append(c);
                }
                else
                {
                    return false;
                }
            }

            if (builder.Length == 0)
            {
                return false;
            }

            fileStem = builder.ToString();
            return true;
        }

        public static string BuildJpegFileName(string fileStem)
        {
            return fileStem + ".jpg";
        }

        public static string BuildMetaFileName(string fileStem)
        {
            return fileStem + ".jpg.meta";
        }

        public static string BuildTextureUrl(string fileStem, string revision)
        {
            string path = "/assets/bodies/" + BuildJpegFileName(fileStem);

            if (string.IsNullOrEmpty(revision))
            {
                return path;
            }

            return path + "?rev=" + revision;
        }
    }
}
