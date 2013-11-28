using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions.BrowserLink.Menu
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class MenuBrowserLinkFactory : IBrowserLinkExtensionFactory
    {
        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.EnableBrowserLinkMenu))
                return null;

            return new MenuBrowserLink();
        }

        public string GetScript()
        {
            if (!WESettings.GetBoolean(WESettings.Keys.EnableBrowserLinkMenu))
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
            Browsers.Client(connection).Invoke("setVisibility", WESettings.GetBoolean(WESettings.Keys.BrowserLink_ShowMenu));
        }

        [BrowserLinkCallback] // This method can be called from JavaScript
        public void ToggleVisibility(bool visible)
        {
            Settings.SetValue(WESettings.Keys.BrowserLink_ShowMenu, visible);
            Settings.Save();
        }
    }
}