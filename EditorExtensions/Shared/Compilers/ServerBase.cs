using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public abstract class ServerBase : IDisposable
    {

        protected string _authenticationToken;
        protected Process _process;
        protected HttpClient _client;
        protected string _address;
        protected int _port;

        public ServerBase()
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

        protected virtual string HeartbeatCheckPath
        {
            get
            {
                return "";
            }
        }

        protected abstract string ProcessStartArgumentsFormat
        {
            get;
        }

        protected abstract string ServerPath
        {
            get;
        }

        private void Initialize()
        {
            var serverPath = ServerPath;

            byte[] randomNumber = new byte[32];

            using (RandomNumberGenerator crypto = RNGCryptoServiceProvider.Create())
                crypto.GetBytes(randomNumber);

            _authenticationToken = Convert.ToBase64String(randomNumber);

            ProcessStartInfo start = new ProcessStartInfo(serverPath)
            {
                WorkingDirectory = Path.GetDirectoryName(serverPath),
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = string.Format(CultureInfo.InvariantCulture, ProcessStartArgumentsFormat, _port, _authenticationToken, Process.GetCurrentProcess().Id),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = Process.Start(start);
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

        protected static async Task<T> Up<T>(T _server)
            where T : ServerBase, new()
        {
            AsyncLock mutex = new AsyncLock();

            if (await HeartbeatCheck(_server))
                return _server;

            while (true)
            {
                using (await mutex.LockAsync())
                {
                    if (_server == null || _server._process == null || _server._process.HasExited)
                        _server = new T();
                }

                using (Task task = Task.Delay(200))
                {
                    await task.ConfigureAwait(false);

                    if (await HeartbeatCheck(_server))
                        break;
                }
            }

            return _server;
        }

        protected static void Down<T>(T _server)
            where T : ServerBase, new()
        {
            if (_server != null && !_server._process.HasExited)
            {
                _server._process.Kill();
                _server._process.Dispose();
                _server.Dispose();
            }
        }

        private static async Task<bool> HeartbeatCheck<T>(T _server)
            where T : ServerBase, new()
        {
            if (_server == null) return false;
            try
            {
                await _server.CallWebServer(_server._address + _server.HeartbeatCheckPath);
                return true;
            }
            catch { return false; }
        }

        protected async Task<CompilerResult> CallService(string path, bool reattempt)
        {
            string newPath = string.Format("{0}?{1}", _address, path);
            HttpResponseMessage response;
            try
            {
                response = await CallWebServer(newPath);

                // Retry once.
                if (!response.IsSuccessStatusCode && !reattempt)
                    return await RetryOnce(path);

                var responseData = await response.Content.ReadAsAsync<NodeServerUtilities.Response>();

                return await responseData.GetCompilerResult();
            }
            catch
            {
                Logger.Log("Something went wrong reaching: " + Uri.EscapeUriString(newPath));
            }

            // Retry once.
            if (!reattempt)
                return await RetryOnce(path);

            return null;
        }

        private async Task<CompilerResult> RetryOnce(string path)
        {
            return await NodeServer.CallServiceAsync(path, true);
        }

        // Making this a separate method so it can throw to caller
        // which is a test criterion for HearbeatCheck.
        private async Task<HttpResponseMessage> CallWebServer(string path)
        {
            return await _client.GetAsync(path).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
