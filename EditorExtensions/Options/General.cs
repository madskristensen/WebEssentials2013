using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class GeneralOptions : DialogPage
    {
        public GeneralOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.KeepImportantComments, KeepImportantComments);
            Settings.SetValue(WESettings.Keys.EnableBrowserLinkMenu, EnableBrowserLinkMenu);
            Settings.SetValue(WESettings.Keys.AllMessagesToOutputWindow, AllMessagesToOutputWindow);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            KeepImportantComments = WESettings.GetBoolean(WESettings.Keys.KeepImportantComments);
            EnableBrowserLinkMenu = WESettings.GetBoolean(WESettings.Keys.EnableBrowserLinkMenu);
            AllMessagesToOutputWindow = WESettings.GetBoolean(WESettings.Keys.AllMessagesToOutputWindow);
        }

        // MISC
        [LocDisplayName("Keep important comments")]
        [Description("Don't strip important comments when minifying JS and CSS. Important comments follows this pattern: /*! text */")]
        [Category("Minification")]
        public bool KeepImportantComments { get; set; }

        [LocDisplayName("Enable Browser Link menu")]
        [Description("Enable the menu that shows up in the browser. Requires restart.")]
        [Category("Browser Link")]
        public bool EnableBrowserLinkMenu { get; set; }

        [LocDisplayName("Redirect Messages to Output Window")]
        [Description("Redirect messages/notifications to output window.")]
        [Category("Messages")]
        public bool AllMessagesToOutputWindow { get; set; }
    }
}
