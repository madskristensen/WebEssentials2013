using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

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
            Settings.SetValue(WESettings.Keys.EnableIcedCoffeeScript, EnableIcedCoffeeScript);
            Settings.SetValue(WESettings.Keys.CoffeeScriptCompileToFolder, CoffeeScriptCompileToFolder);
            Settings.SetValue(WESettings.Keys.CoffeeScriptCompileOnBuild, CoffeeScriptCompileOnBuild);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            GenerateJsFileFromCoffeeScript = WESettings.GetBoolean(WESettings.Keys.GenerateJsFileFromCoffeeScript);
            ShowCoffeeScriptPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowCoffeeScriptPreviewWindow);
            WrapCoffeeScriptClosure = WESettings.GetBoolean(WESettings.Keys.WrapCoffeeScriptClosure);
            CoffeeScriptMinify = WESettings.GetBoolean(WESettings.Keys.CoffeeScriptMinify);
            EnableIcedCoffeeScript = WESettings.GetBoolean(WESettings.Keys.EnableIcedCoffeeScript);
            CoffeeScriptCompileToFolder = WESettings.GetBoolean(WESettings.Keys.CoffeeScriptCompileToFolder);
            CoffeeScriptCompileOnBuild = WESettings.GetBoolean(WESettings.Keys.CoffeeScriptCompileOnBuild);
        }

        [LocDisplayName("Generate JavaScript file on save")]
        [Description("Generate JavaScript file when CoffeeScript file is saved")]
        [Category("CoffeeScript")]
        public bool GenerateJsFileFromCoffeeScript { get; set; }

        [LocDisplayName("Show preview window")]
        [Description("Show the preview window when editing a CoffeeScript file.")]
        [Category("CoffeeScript")]
        public bool ShowCoffeeScriptPreviewWindow { get; set; }

        [LocDisplayName("Wrap generated JavaScript")]
        [Description("Wrap the generated JavaScript in an anonymous function.")]
        [Category("CoffeeScript")]
        public bool WrapCoffeeScriptClosure { get; set; }

        [LocDisplayName("Enable Iced CoffeeScript")]
        [Description("Switches to use the Iced CoffeeScript compiler.")]
        [Category("CoffeeScript")]
        public bool EnableIcedCoffeeScript { get; set; }

        [LocDisplayName("Minify generated JavaScript")]
        [Description("Creates a minified version of the compiled JavaScript file (file.min.js)")]
        [Category("CoffeeScript")]
        public bool CoffeeScriptMinify { get; set; }

        [LocDisplayName("Compile to 'js' folder")]
        [Description("Compiles all CoffeeScript files into a folder called 'js' in the same directory as the .coffee file")]
        [Category("CoffeeScript")]
        public bool CoffeeScriptCompileToFolder { get; set; }

        [LocDisplayName("Compile on build")]
        [Description("Compiles all CoffeeScript files in the project that has a corresponding .js file.")]
        [Category("CoffeeScript")]
        public bool CoffeeScriptCompileOnBuild { get; set; }
    }
}
