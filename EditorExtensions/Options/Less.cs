using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace MadsKristensen.EditorExtensions
{
    class LessOptions : DialogPage
    {
        public LessOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.GenerateCssFileFromLess, GenerateCssFileFromLess);
            Settings.SetValue(WESettings.Keys.ShowLessPreviewWindow, ShowLessPreviewWindow);
            Settings.SetValue(WESettings.Keys.LessMinify, LessMinify);
            Settings.SetValue(WESettings.Keys.LessCompileOnBuild, LessCompileOnBuild);
            Settings.SetValue(WESettings.Keys.LessCompileToFolder, LessCompileToFolder);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            GenerateCssFileFromLess = WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromLess);
            ShowLessPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowLessPreviewWindow);
            LessMinify = WESettings.GetBoolean(WESettings.Keys.LessMinify);
            LessCompileOnBuild = WESettings.GetBoolean(WESettings.Keys.LessCompileOnBuild);
            LessCompileToFolder = WESettings.GetBoolean(WESettings.Keys.LessCompileToFolder);
        }

        [LocDisplayName("Generate CSS file on save")]
        [Description("Generate CSS file when LESS file is saved")]
        [Category("LESS")]
        public bool GenerateCssFileFromLess { get; set; }

        [LocDisplayName("Minify generated CSS files on save")]
        [Description("Creates a minified version of the compiled CSS file (file.min.css).")]
        [Category("LESS")]
        public bool LessMinify { get; set; }

        [LocDisplayName("Show preview window")]
        [Description("Shows the preview window when editing a LESS file.")]
        [Category("LESS")]
        public bool ShowLessPreviewWindow { get; set; }

        [LocDisplayName("Compile on build")]
        [Description("Compiles all LESS files in the project that have a corresponding .css file.")]
        [Category("LESS")]
        public bool LessCompileOnBuild { get; set; }

        [LocDisplayName("Compile to 'css' folder")]
        [Description("Compiles each LESS file into a folder called 'css' in the same directory as the .less file.")]
        [Category("LESS")]
        public bool LessCompileToFolder { get; set; }
    }
}
