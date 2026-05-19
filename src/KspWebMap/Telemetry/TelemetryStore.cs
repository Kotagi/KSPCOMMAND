using System.Threading;

namespace KspWebMap
{
    public sealed class TelemetryStore
    {
        private readonly object _syncRoot = new object();
        private long _nextSnapshotId;
        private TelemetrySnapshot _latest;

        public TelemetryStore()
        {
            _latest = TelemetrySnapshot.CreateInvalid(0, "Telemetry has not captured a snapshot yet.");
        }

        public long NextSnapshotId()
        {
            return Interlocked.Increment(ref _nextSnapshotId);
        }

        public void Publish(TelemetrySnapshot snapshot)
        {
            lock (_syncRoot)
            {
                _latest = snapshot;
            }
        }

        public TelemetrySnapshot GetLatest()
        {
            lock (_syncRoot)
            {
                return _latest;
            }
        }
    }
}
