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
            Settings.SetValue(WESettings.Keys.EnableHtmlMinification, EnableHtmlMinification);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableEnterFormat = WESettings.GetBoolean(WESettings.Keys.EnableEnterFormat);
            EnableAngularValidation = WESettings.GetBoolean(WESettings.Keys.EnableAngularValidation);
            EnableHtmlMinification = WESettings.GetBoolean(WESettings.Keys.EnableHtmlMinification);
        }

        [LocDisplayName("Auto-format HTML on Enter")]
        [Description("Automatically format HTML documents when pressing Enter.")]
        [Category("General")]
        public bool EnableEnterFormat { get; set; }

        [LocDisplayName("Minify HTML files on save")]
        [Description("When a .html file (foo.html) is saved and a minified version (foo.min.html) exist, the minified file will be updated. Right-click any .html file to generate .min.html file")]
        [Category("General")]
        public bool EnableHtmlMinification { get; set; }

        [LocDisplayName("Enable Angular validation")]
        [Description("Validates the document against Angular best practices.")]
        [Category("Angular")]
        public bool EnableAngularValidation { get; set; }
    }
}
