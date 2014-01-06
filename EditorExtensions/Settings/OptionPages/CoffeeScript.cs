using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class CoffeeScriptOptions : DialogPage
    {
        public CoffeeScriptOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.GenerateJsFileFromCoffeeScript, GenerateJsFileFromCoffeeScript);
            Settings.SetValue(WESettings.Keys.ShowCoffeeScriptPreviewWindow, ShowCoffeeScriptPreviewWindow);
            Settings.SetValue(WESettings.Keys.WrapCoffeeScriptClosure, WrapCoffeeScriptClosure);
            Settings.SetValue(WESettings.Keys.CoffeeScriptMinify, CoffeeScriptMinify);
            Settings.SetValue(WESettings.Keys.CoffeeScriptCompileOnBuild, CoffeeScriptCompileOnBuild);
            Settings.SetValue(WESettings.Keys.CoffeeScriptSourceMaps, CoffeeScriptSourceMaps);
            Settings.SetValue(WESettings.Keys.CoffeeScriptCompileToLocation, CoffeeScriptCompileToLocation ?? string.Empty);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            GenerateJsFileFromCoffeeScript = WESettings.GetBoolean(WESettings.Keys.GenerateJsFileFromCoffeeScript);
            ShowCoffeeScriptPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowCoffeeScriptPreviewWindow);
            WrapCoffeeScriptClosure = WESettings.GetBoolean(WESettings.Keys.WrapCoffeeScriptClosure);
            CoffeeScriptMinify = WESettings.GetBoolean(WESettings.Keys.CoffeeScriptMinify);
            CoffeeScriptCompileOnBuild = WESettings.GetBoolean(WESettings.Keys.CoffeeScriptCompileOnBuild);
            CoffeeScriptSourceMaps = WESettings.GetBoolean(WESettings.Keys.CoffeeScriptSourceMaps);
            CoffeeScriptCompileToLocation = WESettings.GetString(WESettings.Keys.CoffeeScriptCompileToLocation);
        }

        [LocDisplayName("Generate JavaScript file on save")]
        [Description("Generates JavaScript file when CoffeeScript file is saved.")]
        [Category("CoffeeScript")]
        [DefaultValue(true)]
        public bool GenerateJsFileFromCoffeeScript { get; set; }

        [LocDisplayName("Show preview pane")]
        [Description("Shows the preview pane when editing a CoffeeScript file.")]
        [Category("CoffeeScript")]
        [DefaultValue(true)]
        public bool ShowCoffeeScriptPreviewWindow { get; set; }

        [LocDisplayName("Wrap generated JavaScript files on save")]
        [Description("Wraps the generated JavaScript in an anonymous function.")]
        [Category("CoffeeScript")]
        [DefaultValue(false)]
        public bool WrapCoffeeScriptClosure { get; set; }

        [LocDisplayName("Minify generated JavaScript")]
        [Description("Creates a minified version of the compiled JavaScript file (file.min.js).")]
        [Category("CoffeeScript")]
        [DefaultValue(true)]
        public bool CoffeeScriptMinify { get; set; }

        [LocDisplayName("Generate source maps")]
        [Description("Creates a source map when compiling the JavaScript file (file.js.map).")]
        [Category("CoffeeScript")]
        public bool CoffeeScriptSourceMaps { get; set; }

        [LocDisplayName("Compile on build")]
        [Description("Compiles all CoffeeScript files in the project that has a corresponding .js file.")]
        [Category("CoffeeScript")]
        [DefaultValue(false)]
        public bool CoffeeScriptCompileOnBuild { get; set; }

        [LocDisplayName("Compile to a custom folder")]
        [Description("Compiles each CoffeeScript file into a custom folder. Leave empty or `.` to save the compiled .js file to the same directory as the .coffee file. Or, prefix your output directory with a `/` to indicate that it starts at the project's root directory (for example '/js' or '/scripts') - this will apply to ALL .coffee files! Otherwise, a relative path is assumed (starting from the file being compiled) - this may cause the output path to be different for each .coffee file.")]
        [Category("CoffeeScript")]
        [DefaultValue("")]
        public string CoffeeScriptCompileToLocation { get; set; }
    }
}
