using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class TsLintOptions : DialogPage
    {
        public TsLintOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.RunTsLintOnBuild, RunTsLintOnBuild);
            Settings.SetValue(WESettings.Keys.EnableTsLint, EnableTsLint);
            Settings.SetValue(WESettings.Keys.TsLintErrorLocation, (int)ErrorLocation);

            OnChanged();
            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableTsLint = WESettings.GetBoolean(WESettings.Keys.EnableTsLint);
            RunTsLintOnBuild = WESettings.GetBoolean(WESettings.Keys.RunTsLintOnBuild);
            ErrorLocation = (WESettings.Keys.FullErrorLocation)WESettings.GetInt(WESettings.Keys.TsLintErrorLocation);
        }

        public static event EventHandler<EventArgs> Changed;

        protected void OnChanged()
        {
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        [LocDisplayName("Enable TSLint")]
        [Description("Runs TSLint in any open .ts file when saved.")]
        [Category("Common")]
        public bool EnableTsLint { get; set; }

        [LocDisplayName("Run on build")]
        [Description("Runs TSLint on all .ts files in the active project on build")]
        [Category("Common")]
        public bool RunTsLintOnBuild { get; set; }

        [LocDisplayName("Error location")]
        [Description("Determins where to output the TSLint errors")]
        [Category("Common")]
        public WESettings.Keys.FullErrorLocation ErrorLocation { get; set; }
    }
}
