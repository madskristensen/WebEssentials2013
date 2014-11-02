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
    public sealed class RubyScssServer : ServerBase
    {
        private static readonly string _sassServerPath = Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(RubyScssServer).Assembly.Location), @"Resources"), @"Tools\sass.exe");
        private static RubyScssServer _server;

        public static async Task Up()
        {
            _server = await ServerBase.Up(_server);
        }

        public static void Down()
        {
            ServerBase.Down(_server);
        }

        public RubyScssServer()
            : base()
        {
        }

        protected override string HeartbeatCheckPath
        {
            get
            {
                return "status";
            }
        }

        protected override string ProcessStartArgumentsFormat
        {
            get
            {
                return @"start {0} {1} {2}";
            }
        }

        protected override string ServerPath
        {
            get
            {
                return _sassServerPath;
            }
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
