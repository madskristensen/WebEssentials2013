using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

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
            Settings.SetValue(WESettings.Keys.EnableEnterFormat, EnableEnterFormat);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            KeepImportantComments = WESettings.GetBoolean(WESettings.Keys.KeepImportantComments);
            EnableEnterFormat = WESettings.GetBoolean(WESettings.Keys.EnableEnterFormat);
        }

        // MISC
        [LocDisplayName("Keep important comments")]
        [Description("Don't strip important comments when minifying JS and CSS. Important comments follows this pattern: /*! text */")]
        [Category("Minification")]
        public bool KeepImportantComments { get; set; }

        [LocDisplayName("Auto-format HTML on Enter")]
        [Description("Automatically format HTML documents when pressing Enter.")]
        [Category("HTML")]
        public bool EnableEnterFormat { get; set; }
    }
}
