using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class BrowserLinkOptions : DialogPage
    {
        public BrowserLinkOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.UnusedCss_IgnorePatterns, IgnorePatterns);
            Settings.SetValue(WESettings.Keys.EnableBrowserLinkMenu, EnableBrowserLinkMenu);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            IgnorePatterns = WESettings.GetString(WESettings.Keys.UnusedCss_IgnorePatterns);
            EnableBrowserLinkMenu = WESettings.GetBoolean(WESettings.Keys.EnableBrowserLinkMenu);
        }

        [LocDisplayName("CSS usage files to ignore")]
        [Description("A semicolon-separated list of file patterns to ignore.")]
        [Category("Browser Link")]
        public string IgnorePatterns { get; set; }

        [LocDisplayName("Enable Browser Link menu")]
        [Description("Enable the menu that shows up in the browser. Requires restart.")]
        [Category("Browser Link")]
        public bool EnableBrowserLinkMenu { get; set; }

        protected override void OnApply(DialogPage.PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (SettingsUpdated != null)
            {
                SettingsUpdated(null, null);
            }
        }

        public static event EventHandler SettingsUpdated;
    }
}
