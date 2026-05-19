using System;
using System.IO;
using System.Reflection;

namespace KspWebMap
{
    public sealed class ServerConfig
    {
        public bool Enabled { get; private set; }

        public string BindAddress { get; private set; }

        public int Port { get; private set; }

        public string WebRoot { get; private set; }

        public string BaseUrl
        {
            get { return string.Format("http://{0}:{1}/", BindAddress, Port); }
        }

        public static ServerConfig CreateDefault()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string pluginDirectory = Path.GetDirectoryName(assemblyPath);
            string modDirectory = Path.GetFullPath(Path.Combine(pluginDirectory, ".."));
            string webRoot = Path.GetFullPath(Path.Combine(modDirectory, "Web"));

            return new ServerConfig
            {
                Enabled = true,
                BindAddress = "127.0.0.1",
                Port = 8750,
                WebRoot = webRoot
            };
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(BindAddress))
            {
                throw new InvalidOperationException("Server bind address is required.");
            }

            if (BindAddress != "127.0.0.1" && BindAddress != "localhost")
            {
                throw new InvalidOperationException("Server bind address must be loopback for this phase.");
            }

            if (Port < 1024 || Port > 65535)
            {
                throw new InvalidOperationException("Server port must be between 1024 and 65535.");
            }

            if (string.IsNullOrEmpty(WebRoot))
            {
                throw new InvalidOperationException("Server web root is required.");
            }
        }
    }
}
