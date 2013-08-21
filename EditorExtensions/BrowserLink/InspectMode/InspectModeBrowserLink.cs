using Microsoft.VisualStudio.Web.BrowserLink;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(BrowserLinkExtensionFactory))]
    [BrowserLinkFactoryName("InspectMode")] // Not needed in final version of VS2013
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

        public IEnumerable<BrowserLinkAction> Actions
        {
            get
            {
                yield return new BrowserLinkAction("Inspect Mode", InitiateInspectMode);
            }
        }

        private void InitiateInspectMode()
        {
            Clients.Call(_connection, "setInspectMode", true);
            _instance = this;
        }

        public static void Select(string sourcePath, int position)
        {
            if (IsInspectModeEnabled)
            {
                _instance.Clients.Call(_instance._connection, "select", sourcePath, position);
            }
        }

        public static bool IsInspectModeEnabled
        {
            get { return _instance != null; }
        }


        [BrowserLinkCallback]
        public void SetInspectMode()
        {
            _instance = this;
        }

        [BrowserLinkCallback]
        public void DisableInspectMode()
        {
            _instance = null;
        }
    }
}