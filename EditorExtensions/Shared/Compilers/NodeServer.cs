using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace MadsKristensen.EditorExtensions
{
    public class NodeServer : IDisposable
    {
        private static readonly string _nodePath = Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(NodeExecutorBase).Assembly.Location), @"Resources"), @"nodejs\node.exe");
        private static readonly AsyncReaderWriterLock _rwLock = new AsyncReaderWriterLock();
        private string _authenticationToken;
        private static NodeServer _server;
        private HttpClient _client;
        private Process _process;
        private Random _random;
        private int _port;

        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("kernel32")]
        extern static UInt64 GetTickCount64();

        public static async Task Up()
        {
            if (_server != null && _server._process != null && !_server._process.HasExited)
                return;

            using (await _rwLock.ReadLockAsync())
                while (_server == null || _server._process == null || _server._process.HasExited)
                    _server = new NodeServer();
        }

        public static async Task<string> CallServiceAsync(string parameters)
        {
            await Up();
            return await _server.CallService(parameters);
        }

        private NodeServer()
        {
            _random = new Random();
            _port = _random.Next(1024, 65535);
            _client = new HttpClient();

            _client.DefaultRequestHeaders.Add("origin", "web essentials");
            _client.DefaultRequestHeaders.Add("user-agent", "web essentials");
            _client.DefaultRequestHeaders.Add("web-essentials", "web essentials");
            _client.DefaultRequestHeaders.Add("auth", _authenticationToken);

            Initialize();
        }

        private void Initialize()
        {
            _authenticationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            ProcessStartInfo start = new ProcessStartInfo(_nodePath)
            {
                WorkingDirectory = Path.GetDirectoryName(_nodePath),
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = string.Format("tools\server\we-nodejs-server.js --port {0} --anti-forgery-token {1} --environment production", _port, _authenticationToken),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = Process.Start(start);
        }

        private async Task<string> CallService(string parameters)
        {
            _client.DefaultRequestHeaders.Add("uptime", TimeSpan.FromMilliseconds(GetTickCount64()).TotalSeconds.ToString());

            parameters = string.Format("http://127.0.0.1:{0}/{1}", _port, parameters);

            try
            {
                HttpResponseMessage response = await _client.GetAsync(parameters);

                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                Logger.Log("Something went wrong reaching: " + parameters);
            }

            // Try again?
            //return await CallServiceAsync(parameters);
            return await Task.FromResult(String.Empty);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _process.Dispose();
                _client.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
