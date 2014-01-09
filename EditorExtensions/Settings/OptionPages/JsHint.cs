using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class JsHintOptions : DialogPage
    {
        public JsHintOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.RunJsHintOnBuild, RunJsHintOnBuild);
            Settings.SetValue(WESettings.Keys.EnableJsHint, EnableJsHint);
            Settings.SetValue(WESettings.Keys.JsHintErrorLocation, (int)ErrorLocation);

            OnChanged();
            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableJsHint = WESettings.GetBoolean(WESettings.Keys.EnableJsHint);
            RunJsHintOnBuild = WESettings.GetBoolean(WESettings.Keys.RunJsHintOnBuild);
            ErrorLocation = (WESettings.Keys.FullErrorLocation)WESettings.GetInt(WESettings.Keys.JsHintErrorLocation);
        }

        public static event EventHandler<EventArgs> Changed;

        protected void OnChanged()
        {
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        [LocDisplayName("Enable JSHint")]
        [Description("Runs JSHint in any open .js file when saved.")]
        [Category("Common")]
        public bool EnableJsHint { get; set; }

        [LocDisplayName("Run on build")]
        [Description("Runs JSHint on all .js files in the active project on build")]
        [Category("Common")]
        public bool RunJsHintOnBuild { get; set; }

        [LocDisplayName("Error location")]
        [Description("Determins where to output the JSHint errors")]
        [Category("Common")]
        public WESettings.Keys.FullErrorLocation ErrorLocation { get; set; }
    }
}
