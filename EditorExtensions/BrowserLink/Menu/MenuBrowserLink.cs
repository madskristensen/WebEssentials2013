using Microsoft.VisualStudio.Web.BrowserLink;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;

namespace MadsKristensen.EditorExtensions.BrowserLink.Menu
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class MenuBrowserLinkFactory : IBrowserLinkExtensionFactory
    {
        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            return new MenuBrowserLink();
        }

        public string GetScript()
        {
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
            Browsers.Client(connection).Invoke("setVisibility", Settings.GetValue(WESettings.Keys.BrowserLink_ShowMenu) ?? true);
        }

        [BrowserLinkCallback] // This method can be called from JavaScript
        public void ToggleVisibility(bool visible)
        {
            Settings.SetValue(WESettings.Keys.BrowserLink_ShowMenu, visible);
        }
    }
}