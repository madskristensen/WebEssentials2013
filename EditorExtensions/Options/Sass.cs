using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class SassOptions : DialogPage
    {
        public SassOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.GenerateCssFileFromSass, GenerateCssFileFromSass);
            Settings.SetValue(WESettings.Keys.ShowSassPreviewWindow, ShowSassPreviewWindow);
            Settings.SetValue(WESettings.Keys.SassMinify, SassMinify);
            Settings.SetValue(WESettings.Keys.SassCompileOnBuild, SassCompileOnBuild);
            Settings.SetValue(WESettings.Keys.SassSourceMaps, SassSourceMaps);
            Settings.SetValue(WESettings.Keys.SassEnableCompiler, SassEnableCompiler);
            Settings.SetValue(WESettings.Keys.SassCompileToLocation, SassCompileToLocation ?? string.Empty);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            GenerateCssFileFromSass = WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromSass);
            ShowSassPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowSassPreviewWindow);
            SassMinify = WESettings.GetBoolean(WESettings.Keys.SassMinify);
            SassCompileOnBuild = WESettings.GetBoolean(WESettings.Keys.SassCompileOnBuild);
            SassSourceMaps = WESettings.GetBoolean(WESettings.Keys.SassSourceMaps);
            SassEnableCompiler = WESettings.GetBoolean(WESettings.Keys.SassEnableCompiler);
            SassCompileToLocation = WESettings.GetString(WESettings.Keys.SassCompileToLocation);
        }

        [LocDisplayName("Generate CSS file on save")]
        [Description("Generate CSS file when SASS file is saved")]
        [Category("SASS")]
        public bool GenerateCssFileFromSass { get; set; }

        [LocDisplayName("Minify generated CSS files on save")]
        [Description("Creates a minified version of the compiled CSS file (file.min.css).")]
        [Category("SASS")]
        public bool SassMinify { get; set; }

        [LocDisplayName("Generate source maps")]
        [Description("Creates a source map when compiling the CSS file (file.css.map).")]
        [Category("SASS")]
        public bool SassSourceMaps { get; set; }

        [LocDisplayName("Show preview window")]
        [Description("Shows the preview window when editing a SASS file.")]
        [Category("SASS")]
        public bool ShowSassPreviewWindow { get; set; }

        [LocDisplayName("Compile on build")]
        [Description("Compiles all SASS files in the project that have a corresponding .css file.")]
        [Category("SASS")]
        public bool SassCompileOnBuild { get; set; }

        [LocDisplayName("Enable SASS compiler")]
        [Description("Enables compiling SASS files. When false, no SASS files will be compiled to CSS, including during a build.")]
        [Category("SASS")]
        public bool SassEnableCompiler { get; set; }

        [LocDisplayName("Compile to a custom folder")]
        [Description("Compiles each SASS file into a custom folder. Leave empty to save the compiled .css file to the same directory as the .scss file. Or, prefix your output directory with a `/` to indicate that it starts at the project's root directory (for example '/css' or '/styles') - this will apply to ALL .scss files! Otherwise, a relative path is assumed (starting from the file being compiled) - this may cause the output path to be different for each .scss file.")]
        [Category("SASS")]
        public string SassCompileToLocation { get; set; }
    }
}
