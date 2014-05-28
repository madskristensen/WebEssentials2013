using Microsoft.VisualStudio.Web.BrowserLink;
using System.ComponentModel.Composition;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Web;

namespace MadsKristensen.EditorExtensions.BrowserLink
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class BrowserInfoFactory : IBrowserLinkExtensionFactory
    {
        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            return new BrowserInfo();
        }

        public string GetScript()
        {
            using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.BrowserInfo.BrowserInfo.js"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class BrowserInfo : BrowserLinkExtension
    {
        public static Dictionary<string, BrowserCap> _infos = new Dictionary<string, BrowserCap>();
        private BrowserLinkConnection _current;

        public override void OnConnected(BrowserLinkConnection connection)
        {
            _current = connection;
            base.OnConnected(connection);
        }

        public override void OnDisconnecting(BrowserLinkConnection connection)
        {
            if (_infos.ContainsKey(connection.ConnectionId))
                _infos.Remove(connection.ConnectionId);

            base.OnDisconnecting(connection);
        }

        [BrowserLinkCallback] // This method can be called from JavaScript
        public void CollectInfo(string name, int width, int height)
        {
            var browserCap = new BrowserCap
            {
                Name = name,
                Width = width,
                Height = height,
            };

            _infos[_current.ConnectionId] = browserCap;
        }
    }

    public class BrowserCap
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}