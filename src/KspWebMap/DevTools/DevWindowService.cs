using UnityEngine;

namespace KspWebMap
{
    public sealed class DevWindowService : IKspWebMapService
    {
        private readonly GameObject _host;
        private HelloWorldWindow _window;

        public DevWindowService(GameObject host)
        {
            _host = host;
        }

        public string Name
        {
            get { return "Dev Window"; }
        }

        public void Start()
        {
            if (_window == null)
            {
                _window = _host.AddComponent<HelloWorldWindow>();
            }
        }

        public void Stop()
        {
            if (_window != null)
            {
                Object.Destroy(_window);
                _window = null;
            }
        }
    }
}
