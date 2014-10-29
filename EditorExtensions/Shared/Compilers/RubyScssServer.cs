using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading.Tasks;
namespace MadsKristensen.EditorExtensions
{
    public sealed class RubyScssServer : IDisposable
    {
        private static readonly string _sassServerPath = Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(RubyScssServer).Assembly.Location), @"Resources"), @"Tools\sass.exe");
        private string _authenticationToken;
        private static RubyScssServer _server;
        private HttpClient _client;
        private Process _process;
        private string _address;
        private int _port;

        public static async Task Up()
        {
            AsyncLock mutex = new AsyncLock();

            if (await HeartbeatCheck())
                return;

            while (true)
            {
                using (await mutex.LockAsync())
                {
                    if (_server == null || _server._process == null || _server._process.HasExited)
                        _server = new RubyScssServer();
                }

                using (Task task = Task.Delay(200))
                {
                    await task.ConfigureAwait(false);

                    if (await HeartbeatCheck())
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

        private static async Task<bool> HeartbeatCheck()
        {
            if (_server == null) return false;
            try
            {
                await _server.CallWebServer(_server._address + "/status");
                return true;
            }
            catch { return false; }
        }

        public RubyScssServer()
        {
            SelectAvailablePort();
            _address = string.Format(CultureInfo.InvariantCulture, "http://127.0.0.1:{0}/", _port);
            _client = new HttpClient();

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

            ProcessStartInfo start = new ProcessStartInfo(_sassServerPath)
            {
                WorkingDirectory = Path.GetDirectoryName(_sassServerPath),
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = string.Format(CultureInfo.InvariantCulture, @"start {0} {1}", _port, _authenticationToken),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = Process.Start(start);
        }

        private async Task<HttpResponseMessage> CallWebServer(string path)
        {
            return await _client.GetAsync(path);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (!_process.HasExited)
                        _process.Kill();
                }
                catch (InvalidOperationException e) { }
                catch { throw; }

                _client.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static string AuthenticationToken
        {
            get
            {
                return _server._authenticationToken;
            }
        }

        public static int Port
        {
            get
            {
                return _server._port;
            }
        }
    }
}
