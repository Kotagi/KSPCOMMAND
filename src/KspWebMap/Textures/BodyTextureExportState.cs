namespace KspWebMap
{
    public sealed class BodyTextureExportState
    {
        public const string StatusPending = "pending";
        public const string StatusReady = "ready";
        public const string StatusFailed = "failed";
        public const string StatusUnsupported = "unsupported";

        public string BodyName;
        public string Status;
        public string Revision;
        public string Url;
        public string RelativePath;
        public string LastError;
        public string MaterialFingerprint;
        public bool RetryAttempted;
    }
}
