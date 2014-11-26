using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public sealed class RubyScssServer : ServerBase
    {
        private static RubyScssServer _server;

        public RubyScssServer()
            : base(@"start {0} {1} {2}",
                   Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(RubyScssServer).Assembly.Location), @"Resources"), @"Tools\sass.exe"))
        { }

        public static async Task Up()
        {
            _server = await ServerBase.Up(_server);
        }

        public static void Down()
        {
            ServerBase.Down(_server);
        }

        protected override string HeartbeatCheckPath { get { return "status"; } }

        public static string AuthenticationToken { get { return _server.BaseAuthenticationToken; } }

        public static int Port { get { return _server.BasePort; } }
    }
}
