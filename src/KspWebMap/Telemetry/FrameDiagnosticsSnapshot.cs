using System;

namespace KspWebMap
{
    public sealed class FrameDiagnosticsSnapshot
    {
        public string ResolverVersion;
        public string OrbitOffsetMode;
        public string VesselOffsetMode;
        public string PluginBuildUtc;
        public double CaptureDurationMs;
        public int BodiesCaptured;
        public int BodyPathsCaptured;
    }
}
