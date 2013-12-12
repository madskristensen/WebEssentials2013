using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class HtmlOptions : DialogPage
    {
        public HtmlOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.EnableEnterFormat, EnableEnterFormat);
            Settings.SetValue(WESettings.Keys.EnableAngularValidation, EnableAngularValidation);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableEnterFormat = WESettings.GetBoolean(WESettings.Keys.EnableEnterFormat);
            EnableAngularValidation = WESettings.GetBoolean(WESettings.Keys.EnableAngularValidation);
        }

        [LocDisplayName("Auto-format HTML on Enter")]
        [Description("Automatically format HTML documents when pressing Enter.")]
        [Category("General")]
        public bool EnableEnterFormat { get; set; }

        [LocDisplayName("Enable Angular validation")]
        [Description("Validates the document against Angular best practices.")]
        [Category("Angular")]
        public bool EnableAngularValidation { get; set; }
    }
}
