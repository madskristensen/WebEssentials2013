using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class JsHintOptions : DialogPage
    {
        public JsHintOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        protected override void OnDeactivate(CancelEventArgs e)
        {
            var error = GetIgnoreListErrors(IgnoreFiles);

            if (!string.IsNullOrEmpty(error))
                MessageBox.Show(error, "Web Essentials", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            base.OnDeactivate(e);
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Text.RegularExpressions.Regex")]
        private static string GetIgnoreListErrors(string source)
        {
            foreach (var pattern in source.Split(';'))
            {
                try
                {
                    new Regex(pattern);
                }
                catch (Exception ex)
                {
                    return "The entry '" + pattern + "' in the Ignore Files list is not a valid regex and will be skipped.\n\n" + ex.Message;
                }
            }

            return null;
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.RunJsHintOnBuild, RunJsHintOnBuild);
            Settings.SetValue(WESettings.Keys.EnableJsHint, EnableJsHint);
            Settings.SetValue(WESettings.Keys.JsHint_ignoreFiles, IgnoreFiles);
            Settings.SetValue(WESettings.Keys.JsHintErrorLocation, (int)ErrorLocation);

            OnChanged();
            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableJsHint = WESettings.GetBoolean(WESettings.Keys.EnableJsHint);
            IgnoreFiles = WESettings.GetString(WESettings.Keys.JsHint_ignoreFiles);
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

        [LocDisplayName("Ignore files")]
        [Description("A semicolon separated list of file name regex's to ignore")]
        [Category("Common")]
        public string IgnoreFiles { get; set; }

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
