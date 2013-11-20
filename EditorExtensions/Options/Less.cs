using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

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
            Settings.SetValue(WESettings.Keys.LessSourceMaps, LessSourceMaps);
            Settings.SetValue(WESettings.Keys.LessEnableCompiler, LessEnableCompiler);
            Settings.SetValue(WESettings.Keys.LessCompileToLocation, LessCompileToLocation);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            GenerateCssFileFromLess = WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromLess);
            ShowLessPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowLessPreviewWindow);
            LessMinify = WESettings.GetBoolean(WESettings.Keys.LessMinify);
            LessCompileOnBuild = WESettings.GetBoolean(WESettings.Keys.LessCompileOnBuild);
            LessSourceMaps = WESettings.GetBoolean(WESettings.Keys.LessSourceMaps);
            LessEnableCompiler = WESettings.GetBoolean(WESettings.Keys.LessEnableCompiler);
            LessCompileToLocation = WESettings.GetString(WESettings.Keys.LessCompileToLocation);
        }

        [LocDisplayName("Generate CSS file on save")]
        [Description("Generate CSS file when LESS file is saved")]
        [Category("LESS")]
        public bool GenerateCssFileFromLess { get; set; }

        [LocDisplayName("Minify generated CSS files on save")]
        [Description("Creates a minified version of the compiled CSS file (file.min.css).")]
        [Category("LESS")]
        public bool LessMinify { get; set; }

        [LocDisplayName("Generate source maps")]
        [Description("Creates a source map when compiling the CSS file (file.css.map).")]
        [Category("LESS")]
        public bool LessSourceMaps { get; set; }

        [LocDisplayName("Show preview window")]
        [Description("Shows the preview window when editing a LESS file.")]
        [Category("LESS")]
        public bool ShowLessPreviewWindow { get; set; }

        [LocDisplayName("Compile on build")]
        [Description("Compiles all LESS files in the project that have a corresponding .css file.")]
        [Category("LESS")]
        public bool LessCompileOnBuild { get; set; }

        [LocDisplayName("Enable LESS compiler")]
        [Description("Enables compiling LESS files. When false, no LESS files will be compiled to CSS, including during a build.")]
        [Category("LESS")]
        public bool LessEnableCompiler { get; set; }

        [LocDisplayName("Compile to a custom folder")]
        [Description("Compiles each LESS file into a custom folder. Leave empty to save the compiled .css file to the same directory as the .less file. Or, prefix your output directory with a `/` to indicate that it starts at the project's root directory (for example '/css' or '/styles') - this will apply to ALL .less files! Otherwise, a relative path is assumed (starting from the file being compiled) - this may cause the output path to be different for each .less file.")]
        [Category("LESS")]
        public string LessCompileToLocation { get; set; }
    }
}
