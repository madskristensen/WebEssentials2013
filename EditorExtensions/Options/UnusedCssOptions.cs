using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

namespace MadsKristensen.EditorExtensions
{
    class UnusedCssOptions : DialogPage
    {
        [LocDisplayName("File patterns to ignore")]
        [Description("A semicolon-separated list of file patterns to ignore.")]
        [Category("CSS")]
        public string IgnorePatterns { get; set; }

        //[LocDisplayName("Ignore remote files")]
        //[Description("Ignore files that are not part of the project")]
        //[Category("CSS")]
        //public bool IgnoreRemoteFiles { get; set; }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.UnusedCss_IgnorePatterns, IgnorePatterns);
            //Settings.SetValue(WESettings.Keys.UnusedCss_IgnoreRemoteFiles, IgnoreRemoteFiles);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            IgnorePatterns = WESettings.GetString(WESettings.Keys.UnusedCss_IgnorePatterns);
            //IgnoreRemoteFiles = WESettings.GetBoolean(WESettings.Keys.UnusedCss_IgnoreRemoteFiles);
        }


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
