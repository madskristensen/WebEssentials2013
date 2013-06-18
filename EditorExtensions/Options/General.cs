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
            Settings.SetValue(WESettings.Keys.EnableMustache, EnableMustache);
            Settings.SetValue(WESettings.Keys.EnableHtmlZenCoding, EnableHtmlZenCoding);
            Settings.SetValue(WESettings.Keys.KeepImportantComments, KeepImportantComments);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableMustache = WESettings.GetBoolean(WESettings.Keys.EnableMustache);
            EnableHtmlZenCoding = WESettings.GetBoolean(WESettings.Keys.EnableHtmlZenCoding);
            KeepImportantComments = WESettings.GetBoolean(WESettings.Keys.KeepImportantComments);
        }

        // MISC
        [LocDisplayName("Enable Mustache/Handlebars")]
        [Description("Enable colorization Mustache/Handlebars syntax in the HTML editor")]
        [Category("Misc")]
        public bool EnableMustache { get; set; }

        [LocDisplayName("Enable HTML ZenCoding")]
        [Description("Enables ZenCoding in the HTML editor")]
        [Category("Misc")]
        public bool EnableHtmlZenCoding { get; set; }

        [LocDisplayName("Keep important comments")]
        [Description("Don't strip important comments when minifying JS and CSS. Important comments follows this pattern: /*! text */")]
        [Category("Minification")]
        public bool KeepImportantComments { get; set; }
    }
}
