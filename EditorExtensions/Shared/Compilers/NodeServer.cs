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
    public sealed class NodeServer : ServerBase
    {
        private static readonly string _nodePath = Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(NodeServer).Assembly.Location), @"Resources"), @"nodejs\node.exe");
        private static NodeServer _server;

        public static async Task Up()
        {
            _server = await ServerBase.Up(_server);
        }

        public static void Down()
        {
            ServerBase.Down(_server);
        }

        public static async Task<CompilerResult> CallServiceAsync(string path, bool reattempt = false)
        {
            await Up();
            return await _server.CallService(path, reattempt);
        }

        public NodeServer()
            : base()
        {
            _client.DefaultRequestHeaders.Add("origin", "web essentials");
            _client.DefaultRequestHeaders.Add("user-agent", "web essentials");
            _client.DefaultRequestHeaders.Add("web-essentials", "web essentials");
            _client.DefaultRequestHeaders.Add("auth", _authenticationToken);
        }

        protected override string ProcessStartArgumentsFormat
        {
            get
            {
                return @"tools\server\we-nodejs-server.js --port {0} --anti-forgery-token {1} --environment production --process-id {2}";
            }
        }

        protected override string ServerPath
        {
            get
            {
                return _nodePath;
            }
        }
    }
}
