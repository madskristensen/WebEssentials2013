using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public sealed class NodeServer : ServerBase
    {
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
            if (_server != null)
                return await _server.CallService(path, reattempt);
            else
                return CompilerResult.GenerateResult(path, "", "", false, "Unable to start node", "", null, false);
        }

        public NodeServer()
            : base(@"tools\server\we-nodejs-server.js --port {0} --anti-forgery-token {1} --environment production --process-id {2}",
                   Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(NodeServer).Assembly.Location), @"Resources"), @"nodejs\node.exe"))
        {
            Client.DefaultRequestHeaders.Add("origin", "web essentials");
            Client.DefaultRequestHeaders.Add("user-agent", "web essentials");
            Client.DefaultRequestHeaders.Add("web-essentials", "web essentials");
            Client.DefaultRequestHeaders.Add("auth", BaseAuthenticationToken);
        }
    }
}
