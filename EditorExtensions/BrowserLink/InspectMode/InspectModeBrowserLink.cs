using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class InspectModeFactory : IBrowserLinkExtensionFactory
    {
        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            return new InspectMode();
        }

        public string GetScript()
        {
            using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.InspectMode.InspectModeBrowserLink.js"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class InspectMode : BrowserLinkExtension
    {
        private BrowserLinkConnection _connection;
        private static InspectMode _instance;

        public override void OnConnected(BrowserLinkConnection connection)
        {
            _connection = connection;

            DisableInspectMode();
        }

        public override void OnDisconnecting(BrowserLinkConnection connection)
        {
            DisableInspectMode();
        }

        public override IEnumerable<BrowserLinkAction> Actions
        {
            get
            {
                yield return new BrowserLinkAction("Inspect Mode", InitiateInspectMode);
            }
        }

        private void InitiateInspectMode(BrowserLinkAction ction)
        {
            Browsers.Client(_connection).Invoke("setInspectMode", true);

            _instance = this;
        }

        public static void Select(string sourcePath, int position)
        {
            if (IsInspectModeEnabled)
            {
                _instance.Browsers.Client(_instance._connection).Invoke("select", sourcePath, position);
            }
        }

        public static bool IsInspectModeEnabled
        {
            get { return _instance != null; }
        }


        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [BrowserLinkCallback]
        public void SetInspectMode()
        {
            _instance = this;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [BrowserLinkCallback]
        public void DisableInspectMode()
        {
            _instance = null;
        }
    }
}