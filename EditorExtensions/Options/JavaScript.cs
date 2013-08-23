using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace MadsKristensen.EditorExtensions
{
    class JavaScriptOptions : DialogPage
    {
        public JavaScriptOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.EnableJavascriptRegions, EnableJavascriptRegions);
            Settings.SetValue(WESettings.Keys.EnableJsMinification, EnableJsMinification);
            Settings.SetValue(WESettings.Keys.JavaScriptEnableGzipping, EnableGzipping);
            Settings.SetValue(WESettings.Keys.GenerateJavaScriptSourceMaps, GenerateJavaScriptSourceMaps);
            Settings.SetValue(WESettings.Keys.JavaScriptAutoCloseBraces, JavaScriptAutoCloseBraces);
            Settings.SetValue(WESettings.Keys.JavaScriptOutlining, JavaScriptOutlining);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableJavascriptRegions = WESettings.GetBoolean(WESettings.Keys.EnableJavascriptRegions);
            EnableJsMinification = WESettings.GetBoolean(WESettings.Keys.EnableJsMinification);
            EnableGzipping = WESettings.GetBoolean(WESettings.Keys.JavaScriptEnableGzipping);
            GenerateJavaScriptSourceMaps = WESettings.GetBoolean(WESettings.Keys.GenerateJavaScriptSourceMaps);
            JavaScriptAutoCloseBraces = WESettings.GetBoolean(WESettings.Keys.JavaScriptAutoCloseBraces);
            JavaScriptOutlining = WESettings.GetBoolean(WESettings.Keys.JavaScriptOutlining);
        }


        [LocDisplayName("Enable JavaScript regions")]
        [Description("Enable regions using this syntax: '//#region Name' followed by '//#endregion'")]
        [Category("JavaScript")]
        public bool EnableJavascriptRegions { get; set; }

        [LocDisplayName("Minify JavaScript files on save")]
        [Description("When a .js file (foo.js) is saved and a minified version (foo.min.js) exist, the minified file will be updated. Right-click any .js file to generate .min.js file")]
        [Category("JavaScript")]
        public bool EnableJsMinification { get; set; }

        [LocDisplayName("Gzip JavaScript files on save")]
        [Description("When a .js file (foo.js) is saved and a minified version (foo.min.js) exist, a gzipped file will be created.")]
        [Category("JavaScript")]
        public bool EnableGzipping { get; set; }

        [LocDisplayName("Generate source maps (.map)")]
        [Description("When minification is enabled, a source map file (*.min.js.map) is generated.")]
        [Category("JavaScript")]
        public bool GenerateJavaScriptSourceMaps { get; set; }

        [LocDisplayName("Auto-close braces")]
        [Description("Automatically inserts closing braces as provisional text. Braces are: ], ) and }")]
        [Category("JavaScript")]
        public bool JavaScriptAutoCloseBraces { get; set; }

        [LocDisplayName("Enable outlining/folding")]
        [Description("Enables outlining for any non-function structures. Enabling can collide with other extensions.")]
        [Category("JavaScript")]
        public bool JavaScriptOutlining { get; set; }
    }
}
