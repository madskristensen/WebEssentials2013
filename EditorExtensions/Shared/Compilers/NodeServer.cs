using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public sealed class NodeServer : IDisposable
    {
        private static readonly string _nodePath = Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(NodeExecutorBase).Assembly.Location), @"Resources"), @"nodejs\node.exe");
        private string _authenticationToken;
        private static NodeServer _server;
        private HttpClient _client;
        private Process _process;
        private string _address;
        private int _port;

        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("kernel32")]
        extern static UInt64 GetTickCount64();

        public static async Task Up()
        {
            AsyncLock mutex = new AsyncLock();

            if (await HearbeatCheck())
                return;

            while (true)
            {
                using (await mutex.LockAsync())
                {
                    if (_server == null || _server._process == null || _server._process.HasExited)
                        _server = new NodeServer();
                }

                using (Task task = Task.Delay(200))
                {
                    await task.ConfigureAwait(false);

                    if (await HearbeatCheck())
                        break;
                }
            }
        }

        public static void Down()
        {
            if (_server != null && !_server._process.HasExited)
            {
                _server._process.Kill();
                _server._process.Dispose();
                _server.Dispose();
            }
        }

        private static async Task<bool> HearbeatCheck()
        {
            try
            {
                await _server.CallWebServer(_server._address);
                return true;
            }
            catch { return false; }
        }

        public static async Task<string> CallServiceAsync(string path)
        {
            await Up();
            return await _server.CallService(path);
        }

        private NodeServer()
        {
            SelectAvailablePort();
            _address = string.Format(CultureInfo.InvariantCulture, "http://127.0.0.1:{0}/", _port);
            _client = new HttpClient();

            _client.DefaultRequestHeaders.Add("origin", "web essentials");
            _client.DefaultRequestHeaders.Add("user-agent", "web essentials");
            _client.DefaultRequestHeaders.Add("web-essentials", "web essentials");
            _client.DefaultRequestHeaders.Add("auth", _authenticationToken);

            Initialize();
        }

        private void SelectAvailablePort()
        {
            Random rand = new Random();
            TcpConnectionInformation[] connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

            do
                _port = rand.Next(1024, 65535);
            while (connections.Any(t => t.LocalEndPoint.Port == _port));
        }

        private void Initialize()
        {
            byte[] randomNumber = new byte[32];

            using (RandomNumberGenerator crypto = RNGCryptoServiceProvider.Create())
                crypto.GetBytes(randomNumber);

            _authenticationToken = Convert.ToBase64String(randomNumber);

            ProcessStartInfo start = new ProcessStartInfo(_nodePath)
            {
                WorkingDirectory = Path.GetDirectoryName(_nodePath),
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = string.Format(CultureInfo.InvariantCulture, @"tools\server\we-nodejs-server.js --port {0} --anti-forgery-token {1} --environment production --process-id {2}", _port, _authenticationToken, Process.GetCurrentProcess().Id),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = Process.Start(start);
        }

        public async Task<string> CallService(string path)
        {
            _client.DefaultRequestHeaders.Add("uptime", TimeSpan.FromMilliseconds(GetTickCount64()).TotalSeconds.ToString());

            path = string.Format("{0}{1}", _address, path);

            try
            {
                HttpResponseMessage response = await CallWebServer(path);

                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                Logger.Log("Something went wrong reaching: " + path);
            }

            // Try again?
            //return await CallServiceAsync(parameters);
            return await Task.FromResult(String.Empty);
        }

        // Making this a separate method so it can throw to caller
        // which is a test criterion for HearbeatCheck.
        private async Task<HttpResponseMessage> CallWebServer(string path)
        {
            return await _client.GetAsync(path);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_process.HasExited)
                    _process.Kill();

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
