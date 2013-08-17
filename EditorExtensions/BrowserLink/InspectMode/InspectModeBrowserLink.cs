using Microsoft.VisualStudio.Web.BrowserLink;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(BrowserLinkExtensionFactory))]
    [BrowserLinkFactoryName("InspectMode")]
    public class InspectModeFactory : BrowserLinkExtensionFactory
    {
        public override BrowserLinkExtension CreateInstance(BrowserLinkConnection connection)
        {
            return new InspectMode();
        }

        public override string Script
        {
            get
            {
                using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.InspectMode.InspectModeBrowserLink.js"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    public class InspectMode : BrowserLinkExtension, IBrowserLinkActionProvider
    {
        BrowserLinkConnection _connection;

        public override void OnConnected(BrowserLinkConnection connection)
        {
            _connection = connection;
        }

        public IEnumerable<BrowserLinkAction> Actions
        {
            get
            {
                yield return new BrowserLinkAction("Inspect Mode", SetInspectMode);
            }
        }

        private void SetInspectMode()
        {
            Clients.Call(_connection, "setInspectMode", true);
        }
    }
}