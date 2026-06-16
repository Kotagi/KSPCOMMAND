namespace KspWebMap
{
    /// <summary>
    /// Bumped when export pixel layout changes so cached JPEGs are invalidated.
    /// v2: horizontal flip at export (web no longer mirrors U on SphereGeometry).
    /// </summary>
    public static class BodyTextureExportLayout
    {
        public const string CurrentLayoutId = "v7-kopernicus-ondemand-moons";

        public static string BuildMetaFileContent(string materialFingerprint)
        {
            return (materialFingerprint ?? string.Empty) + "\n" + CurrentLayoutId;
        }

        public static bool TryParseMetaFileContent(
            string metaText,
            out string materialFingerprint,
            out string layoutId)
        {
            materialFingerprint = null;
            layoutId = null;

            if (string.IsNullOrEmpty(metaText))
            {
                return false;
            }

            string[] lines = metaText.Split(
                new[] { '\r', '\n' },
                System.StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 1)
            {
                return false;
            }

            materialFingerprint = lines[0].Trim();
            layoutId = lines.Length >= 2 ? lines[1].Trim() : string.Empty;
            return materialFingerprint.Length > 0;
        }

        public static bool IsLayoutCurrent(string layoutId)
        {
            return string.Equals(layoutId, CurrentLayoutId, System.StringComparison.Ordinal);
        }
    }
}
