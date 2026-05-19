using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace KspWebMap
{
    public sealed class LocalHttpServerService : IKspWebMapService
    {
        private const string LogPrefix = "[KspWebMap]";
        private readonly ServerConfig _config;
        private readonly TelemetryStore _telemetryStore;
        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _running;
        private string _canonicalWebRoot;

        public LocalHttpServerService(ServerConfig config, TelemetryStore telemetryStore)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (telemetryStore == null)
            {
                throw new ArgumentNullException("telemetryStore");
            }

            _config = config;
            _telemetryStore = telemetryStore;
        }

        public string Name
        {
            get { return "Local HTTP Server"; }
        }

        public void Start()
        {
            if (!_config.Enabled)
            {
                Debug.Log(LogPrefix + " Local HTTP server disabled by configuration.");
                return;
            }

            _config.Validate();
            _canonicalWebRoot = CanonicalizeDirectory(_config.WebRoot);

            try
            {
                IPAddress bindAddress = IPAddress.Parse(_config.BindAddress);
                _listener = new TcpListener(bindAddress, _config.Port);
                _listener.Start();

                _running = true;
                _thread = new Thread(RunServer);
                _thread.IsBackground = true;
                _thread.Name = "KspWebMap.HttpServer";
                _thread.Start();
            }
            catch (Exception ex)
            {
                _running = false;

                if (_listener != null)
                {
                    _listener.Stop();
                    _listener = null;
                }

                Debug.LogError(string.Format(
                    "{0} Local HTTP server failed to start on {1}: {2}",
                    LogPrefix,
                    _config.BaseUrl,
                    ex.Message));
                return;
            }

            Debug.Log(string.Format(
                "{0} Local HTTP server listening on {1} with web root {2}",
                LogPrefix,
                _config.BaseUrl,
                _canonicalWebRoot));
        }

        public void Stop()
        {
            _running = false;

            if (_listener != null)
            {
                _listener.Stop();
                _listener = null;
            }

            if (_thread != null)
            {
                if (!_thread.Join(1000))
                {
                    Debug.LogWarning(LogPrefix + " Local HTTP server thread did not stop within 1000 ms.");
                }

                _thread = null;
            }

            Debug.Log(LogPrefix + " Local HTTP server stopped.");
        }

        private void RunServer()
        {
            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    HandleClient(client);
                }
                catch (SocketException)
                {
                    if (_running)
                    {
                        Debug.LogError(LogPrefix + " Socket error in local HTTP server.");
                    }
                }
                catch (ObjectDisposedException)
                {
                    if (_running)
                    {
                        Debug.LogError(LogPrefix + " Local HTTP listener was disposed unexpectedly.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(string.Format("{0} Local HTTP server error: {1}", LogPrefix, ex));
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            {
                client.ReceiveTimeout = 2000;
                client.SendTimeout = 2000;

                using (NetworkStream stream = client.GetStream())
                {
                    HttpRequest request = ReadRequest(stream);

                    if (request == null)
                    {
                        WriteTextResponse(stream, "400 Bad Request", "text/plain; charset=utf-8", "Bad Request", false);
                        return;
                    }

                    HandleRequest(stream, request);
                }
            }
        }

        private HttpRequest ReadRequest(Stream stream)
        {
            StreamReader reader = new StreamReader(stream, Encoding.ASCII, false, 1024);
            string requestLine = reader.ReadLine();

            if (string.IsNullOrEmpty(requestLine))
            {
                return null;
            }

            string[] parts = requestLine.Split(' ');

            if (parts.Length < 3)
            {
                return null;
            }

            string headerLine;
            do
            {
                headerLine = reader.ReadLine();
            }
            while (!string.IsNullOrEmpty(headerLine));

            return new HttpRequest(parts[0], parts[1]);
        }

        private void HandleRequest(Stream stream, HttpRequest request)
        {
            bool isHead = string.Equals(request.Method, "HEAD", StringComparison.OrdinalIgnoreCase);

            if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase) && !isHead)
            {
                WriteTextResponse(stream, "405 Method Not Allowed", "text/plain; charset=utf-8", "Method Not Allowed", isHead);
                return;
            }

            string path = NormalizeRequestPath(request.Path);

            if (path == "/api/health")
            {
                WriteJsonResponse(stream, BuildHealthJson(), isHead);
                return;
            }

            if (path == "/api/telemetry")
            {
                WriteJsonResponse(stream, TelemetryJsonWriter.WriteTelemetry(_telemetryStore.GetLatest()), isHead);
                return;
            }

            if (path == "/api/active-vessel")
            {
                WriteJsonResponse(stream, TelemetryJsonWriter.WriteActiveVesselEndpoint(_telemetryStore.GetLatest()), isHead);
                return;
            }

            if (path == "/api/orbit")
            {
                WriteJsonResponse(stream, TelemetryJsonWriter.WriteOrbitEndpoint(_telemetryStore.GetLatest()), isHead);
                return;
            }

            if (path == "/api/bodies")
            {
                WriteJsonResponse(stream, TelemetryJsonWriter.WriteBodiesEndpoint(_telemetryStore.GetLatest()), isHead);
                return;
            }

            if (path == "/" || path == "/index.html")
            {
                ServeStaticFile(stream, "index.html", isHead);
                return;
            }

            if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                ServeStaticFile(stream, relativePath, isHead);
                return;
            }

            WriteTextResponse(stream, "404 Not Found", "text/plain; charset=utf-8", "Not Found", isHead);
        }

        private string BuildHealthJson()
        {
            return string.Format(
                "{{\"status\":\"ok\",\"service\":\"KspWebMap\",\"version\":\"0.1.0\",\"serverTimeUtc\":\"{0}\"}}",
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        private void ServeStaticFile(Stream stream, string relativePath, bool headOnly)
        {
            string filePath = Path.GetFullPath(Path.Combine(_canonicalWebRoot, relativePath));

            if (!IsPathInsideWebRoot(filePath))
            {
                WriteTextResponse(stream, "404 Not Found", "text/plain; charset=utf-8", "Not Found", headOnly);
                return;
            }

            if (!File.Exists(filePath))
            {
                WriteTextResponse(stream, "404 Not Found", "text/plain; charset=utf-8", "Not Found", headOnly);
                return;
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            WriteBytesResponse(stream, "200 OK", GetContentType(filePath), bytes, headOnly);
        }

        private string NormalizeRequestPath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath))
            {
                return "/";
            }

            int queryIndex = rawPath.IndexOf('?');

            if (queryIndex >= 0)
            {
                rawPath = rawPath.Substring(0, queryIndex);
            }

            if (!rawPath.StartsWith("/"))
            {
                rawPath = "/" + rawPath;
            }

            return Uri.UnescapeDataString(rawPath);
        }

        private bool IsPathInsideWebRoot(string path)
        {
            return path.StartsWith(_canonicalWebRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static string CanonicalizeDirectory(string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                fullPath += Path.DirectorySeparatorChar;
            }

            return fullPath;
        }

        private static string GetContentType(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();

            switch (extension)
            {
                case ".html":
                    return "text/html; charset=utf-8";
                case ".js":
                    return "application/javascript; charset=utf-8";
                case ".css":
                    return "text/css; charset=utf-8";
                case ".json":
                    return "application/json; charset=utf-8";
                case ".txt":
                    return "text/plain; charset=utf-8";
                case ".map":
                    return "application/json; charset=utf-8";
                case ".png":
                    return "image/png";
                case ".webp":
                    return "image/webp";
                case ".svg":
                    return "image/svg+xml";
                case ".woff2":
                    return "font/woff2";
                default:
                    return "application/octet-stream";
            }
        }

        private static void WriteJsonResponse(Stream stream, string json, bool headOnly)
        {
            WriteTextResponse(stream, "200 OK", "application/json; charset=utf-8", json, headOnly);
        }

        private static void WriteTextResponse(Stream stream, string status, string contentType, string body, bool headOnly)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            WriteBytesResponse(stream, status, contentType, bodyBytes, headOnly);
        }

        private static void WriteBytesResponse(Stream stream, string status, string contentType, byte[] body, bool headOnly)
        {
            string headers = string.Format(
                "HTTP/1.1 {0}\r\nContent-Type: {1}\r\nContent-Length: {2}\r\nConnection: close\r\nCache-Control: no-store\r\n\r\n",
                status,
                contentType,
                body.Length);

            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);

            if (!headOnly)
            {
                stream.Write(body, 0, body.Length);
            }
        }

        private sealed class HttpRequest
        {
            public HttpRequest(string method, string path)
            {
                Method = method;
                Path = path;
            }

            public string Method { get; private set; }

            public string Path { get; private set; }
        }
    }
}
