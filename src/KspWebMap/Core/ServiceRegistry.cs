using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspWebMap
{
    public sealed class ServiceRegistry : IDisposable
    {
        private readonly List<IKspWebMapService> _services = new List<IKspWebMapService>();
        private readonly string _logPrefix;
        private bool _started;

        public ServiceRegistry(string logPrefix)
        {
            _logPrefix = logPrefix;
        }

        public void Add(IKspWebMapService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            if (_started)
            {
                throw new InvalidOperationException("Services cannot be added after startup.");
            }

            _services.Add(service);
        }

        public void StartAll()
        {
            if (_started)
            {
                return;
            }

            List<IKspWebMapService> startedServices = new List<IKspWebMapService>();

            try
            {
                foreach (IKspWebMapService service in _services)
                {
                    Debug.Log(string.Format("{0} Starting service: {1}", _logPrefix, service.Name));
                    service.Start();
                    startedServices.Add(service);
                }

                _started = true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("{0} Service startup failed: {1}", _logPrefix, ex));
                StopStartedServices(startedServices);

                throw;
            }
        }

        public void StopAll()
        {
            if (!_started)
            {
                return;
            }

            for (int i = _services.Count - 1; i >= 0; i--)
            {
                IKspWebMapService service = _services[i];

                try
                {
                    Debug.Log(string.Format("{0} Stopping service: {1}", _logPrefix, service.Name));
                    service.Stop();
                }
                catch (Exception ex)
                {
                    Debug.LogError(string.Format("{0} Service stop failed for {1}: {2}", _logPrefix, service.Name, ex));
                }
            }

            _started = false;
        }

        public void Dispose()
        {
            StopAll();
        }

        private void StopStartedServices(IList<IKspWebMapService> startedServices)
        {
            for (int i = startedServices.Count - 1; i >= 0; i--)
            {
                IKspWebMapService service = startedServices[i];

                try
                {
                    Debug.Log(string.Format("{0} Rolling back service startup: {1}", _logPrefix, service.Name));
                    service.Stop();
                }
                catch (Exception ex)
                {
                    Debug.LogError(string.Format("{0} Service rollback failed for {1}: {2}", _logPrefix, service.Name, ex));
                }
            }
        }
    }
}
