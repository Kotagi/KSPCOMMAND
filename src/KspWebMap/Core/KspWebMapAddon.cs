using UnityEngine;

namespace KspWebMap
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class KspWebMapAddon : MonoBehaviour
    {
        private const string LogPrefix = "[KspWebMap]";
        private static KspWebMapAddon _activeInstance;

        private ServiceRegistry _services;
        private bool _ownsServices;

        private void Awake()
        {
            if (_activeInstance != null && _activeInstance != this)
            {
                Debug.LogWarning(LogPrefix + " Duplicate flight addon detected; destroying duplicate instance.");
                Destroy(this);
                return;
            }

            _activeInstance = this;
            _ownsServices = true;

            ServerConfig serverConfig = ServerConfig.CreateDefault();
            TelemetryStore telemetryStore = new TelemetryStore();
            BodyTextureExportRegistry textureRegistry = new BodyTextureExportRegistry();

            _services = new ServiceRegistry(LogPrefix);
            _services.Add(new DevWindowService(gameObject));
            _services.Add(new BodyTextureExportService(gameObject, serverConfig, textureRegistry));
            // Diagnostic probe disabled — it blits every body/property repeatedly and stalls RSS.
            _services.Add(new TelemetrySnapshotService(gameObject, telemetryStore, textureRegistry));
            _services.Add(new LocalHttpServerService(serverConfig, telemetryStore));
            _services.StartAll();

            Debug.Log(LogPrefix + " Flight addon loaded.");
        }

        private void OnDestroy()
        {
            if (!_ownsServices)
            {
                return;
            }

            if (_services != null)
            {
                _services.Dispose();
                _services = null;
            }

            if (_activeInstance == this)
            {
                _activeInstance = null;
            }

            Debug.Log(LogPrefix + " Flight addon destroyed.");
        }
    }
}
