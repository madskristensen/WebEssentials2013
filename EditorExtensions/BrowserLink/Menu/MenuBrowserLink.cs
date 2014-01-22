using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions.BrowserLink.Menu
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class MenuBrowserLinkFactory : IBrowserLinkExtensionFactory
    {
        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            if (!WESettings.Instance.BrowserLink.EnableMenu)
                return null;

            return new MenuBrowserLink();
        }

        public string GetScript()
        {
            if (!WESettings.Instance.BrowserLink.EnableMenu)
                return null;

            using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.Menu.MenuBrowserLink.js"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class MenuBrowserLink : BrowserLinkExtension
    {
        public override void OnConnected(BrowserLinkConnection connection)
        {
            Browsers.Client(connection).Invoke("setVisibility", WESettings.Instance.BrowserLink.ShowMenu);
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [BrowserLinkCallback] // This method can be called from JavaScript
        public void ToggleVisibility(bool visible)
        {
            WESettings.Instance.BrowserLink.ShowMenu = visible;
            SettingsStore.Save();
        }
    }
}