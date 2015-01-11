using System;
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
    public abstract class ServerBase : IDisposable
    {
        private Process _process;
        private string _address;

        protected int BasePort { get; private set; }
        protected string BaseAuthenticationToken { get; private set; }

        protected ServerBase(string processStartArgumentsFormat, string serverPath)
        {
            SelectAvailablePort();
            _address = string.Format(CultureInfo.InvariantCulture, "http://localhost.:{0}/", BasePort);
            Client = new HttpClient();

            Initialize(processStartArgumentsFormat, serverPath);
        }

        private void SelectAvailablePort()
        {
            Random rand = new Random();
            TcpConnectionInformation[] connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

            do
                BasePort = rand.Next(1024, 65535);
            while (connections.Any(t => t.LocalEndPoint.Port == BasePort));
        }

        protected HttpClient Client { get; set; }

        protected virtual string HeartbeatCheckPath { get { return ""; } }

        private void Initialize(string processStartArgumentsFormat, string serverPath)
        {
            byte[] randomNumber = new byte[32];

            using (RandomNumberGenerator crypto = RNGCryptoServiceProvider.Create())
                crypto.GetBytes(randomNumber);

            BaseAuthenticationToken = Convert.ToBase64String(randomNumber);

            ProcessStartInfo start = new ProcessStartInfo(serverPath)
            {
                WorkingDirectory = Path.GetDirectoryName(serverPath),
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = string.Format(CultureInfo.InvariantCulture, processStartArgumentsFormat,
                                         BasePort, BaseAuthenticationToken, Process.GetCurrentProcess().Id),
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
                catch (InvalidOperationException) { }
                catch { throw; }

                Client.Dispose();
            }
        }

        protected static async Task<T> Up<T>(T server)
            where T : ServerBase, new()
        {
            AsyncLock mutex = new AsyncLock();

            if (await HeartbeatCheck(server))
                return server;

            int tries = 0;
            while (true)
            {
                using (await mutex.LockAsync())
                {
                    if (server == null || server._process == null || server._process.HasExited)
                        server = new T();
                }

                using (Task task = Task.Delay(200))
                {
                    Logger.Log(string.Format("Looking for resource @ {0}", server._address));
                    await task.ConfigureAwait(false);

                    if (await HeartbeatCheck(server))
                        break;
                    else
                    {
                        if(server._process.HasExited)
                        {
                            Logger.Log("Unable to start resource, aborting");
                            server.Dispose();
                            return null;
                        }

                        tries++;
                        if (tries > 5)
                        {
                            Logger.Log("Unable to find resource, aborting");
                            if (!server._process.HasExited)
                                server._process.Kill();

                            return null;
                        }
                    }
                }
            }

            return server;
        }

        protected static void Down<T>(T server)
            where T : ServerBase, new()
        {
            if (server != null && !server._process.HasExited)
            {
                server._process.Kill();
                server._process.Dispose();
                server.Dispose();
            }
        }

        private static async Task<bool> HeartbeatCheck<T>(T _server)
            where T : ServerBase, new()
        {
            if (_server == null) return false;
            try
            {
                HttpResponseMessage response = await _server.CallWebServer(_server._address + _server.HeartbeatCheckPath);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    return true;
                else
                    return false;
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
            return await Client.GetAsync(path).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
