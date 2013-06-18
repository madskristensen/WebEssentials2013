using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace MadsKristensen.EditorExtensions
{
    class ScssOptions : DialogPage
    {
        public ScssOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.GenerateCssFileFromScss, GenerateCssFileFromScss);
            Settings.SetValue(WESettings.Keys.ShowScssPreviewWindow, ShowScssPreviewWindow);
            Settings.SetValue(WESettings.Keys.ScssMinify, ScssMinify);
            Settings.SetValue(WESettings.Keys.ScssCompileOnBuild, ScssCompileOnBuild);
            Settings.SetValue(WESettings.Keys.ScssCompileToFolder, ScssCompileToFolder);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            GenerateCssFileFromScss = WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromScss);
            ShowScssPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowScssPreviewWindow);
            ScssMinify = WESettings.GetBoolean(WESettings.Keys.ScssMinify);
            ScssCompileOnBuild = WESettings.GetBoolean(WESettings.Keys.ScssCompileOnBuild);
            ScssCompileToFolder = WESettings.GetBoolean(WESettings.Keys.ScssCompileToFolder);
        }

        [LocDisplayName("Generate CSS file on save")]
        [Description("Generate CSS file when Scss file is saved")]
        [Category("Scss")]
        public bool GenerateCssFileFromScss { get; set; }

        [LocDisplayName("Generate min file on save")]
        [Description("Creates a minified version of the compiled CSS file (file.min.css)")]
        [Category("Scss")]
        public bool ScssMinify { get; set; }

        [LocDisplayName("Show preview window")]
        [Description("Show the preview window when editing a Scss file.")]
        [Category("Scss")]
        public bool ShowScssPreviewWindow { get; set; }

        [LocDisplayName("Compile on build")]
        [Description("Compiles all Scss files in the project that has a corresponding .css file.")]
        [Category("Scss")]
        public bool ScssCompileOnBuild { get; set; }

        [LocDisplayName("Compile to 'css' folder")]
        [Description("Compiles all SCSS files into a folder called 'css' in the same directory as the .scss file")]
        [Category("Scss")]
        public bool ScssCompileToFolder { get; set; }
    }
}
