using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace MadsKristensen.EditorExtensions
{
    class TypeScriptOptions : DialogPage
    {
        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.GenerateJsFileFromTypeScript, GenerateJsFileFromTypeScript);
            Settings.SetValue(WESettings.Keys.ShowTypeScriptPreviewWindow, ShowTypeScriptPreviewWindow);
            Settings.SetValue(WESettings.Keys.CompileTypeScriptOnBuild, CompileTypeScriptOnBuild);
            Settings.SetValue(WESettings.Keys.TypeScriptKeepComments, TypeScriptKeepComments);
            Settings.SetValue(WESettings.Keys.TypeScriptUseAmdModule, TypeScriptUseAmdModule);
            Settings.SetValue(WESettings.Keys.TypeScriptCompileES3, TypeScriptCompileES3);
            Settings.SetValue(WESettings.Keys.TypeScriptProduceSourceMap, TypeScriptProduceSourceMap);
            Settings.SetValue(WESettings.Keys.TypeScriptMinify, TypeScriptMinify);
            Settings.SetValue(WESettings.Keys.TypeScriptAddGeneratedFilesToProject, TypeScriptAddGeneratedFilesToProject);
            Settings.SetValue(WESettings.Keys.TypeScriptResaveWithUtf8BOM, TypeScriptResaveWithUtf8BOM);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            GenerateJsFileFromTypeScript = WESettings.GetBoolean(WESettings.Keys.GenerateJsFileFromTypeScript);
            ShowTypeScriptPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowTypeScriptPreviewWindow);
            CompileTypeScriptOnBuild = WESettings.GetBoolean(WESettings.Keys.CompileTypeScriptOnBuild);
            TypeScriptKeepComments = WESettings.GetBoolean(WESettings.Keys.TypeScriptKeepComments);
            TypeScriptUseAmdModule = WESettings.GetBoolean(WESettings.Keys.TypeScriptUseAmdModule);
            TypeScriptCompileES3 = WESettings.GetBoolean(WESettings.Keys.TypeScriptCompileES3);
            TypeScriptProduceSourceMap = WESettings.GetBoolean(WESettings.Keys.TypeScriptProduceSourceMap);
            TypeScriptMinify = WESettings.GetBoolean(WESettings.Keys.TypeScriptMinify);
            TypeScriptAddGeneratedFilesToProject = WESettings.GetBoolean(WESettings.Keys.TypeScriptAddGeneratedFilesToProject);
            TypeScriptResaveWithUtf8BOM = WESettings.GetBoolean(WESettings.Keys.TypeScriptResaveWithUtf8BOM);
        }

        [LocDisplayName("Compile TypeScript on save")]
        [Description("Generate JavaScript file when TypeScript file is saved")]
        [Category("TypeScript")]
        public bool GenerateJsFileFromTypeScript { get; set; }

        [LocDisplayName("Show preview window")]
        [Description("Show the preview window when editing a TypeScript file.")]
        [Category("TypeScript")]
        public bool ShowTypeScriptPreviewWindow { get; set; }

        [LocDisplayName("Add generated files to project")]
        [Description("Includes the generated .js, .min.js and .map files to the current project, nested under the .ts file.")]
        [Category("TypeScript")]
        public bool TypeScriptAddGeneratedFilesToProject { get; set; }

        [LocDisplayName("Compile all TypeScript files on build")]
        [Description("Runs the compiler on all TypeScript files in your project on build")]
        [Category("TypeScript")]
        public bool CompileTypeScriptOnBuild { get; set; }

        [LocDisplayName("Minify generated JavaScript")]
        [Description("Creates a minified version of the compiled JavaScript file (file.min.js)")]
        [Category("TypeScript")]
        public bool TypeScriptMinify { get; set; }

        [LocDisplayName("Re-save JS with UTF8 BOM")]
        [Description("Re-saves the compiled output with a UTF-8 BOM")]
        [Category("TypeScript")]
        public bool TypeScriptResaveWithUtf8BOM { get; set; }

        [LocDisplayName("Use the AMD module")]
        [Description("Sets the '--module AMD' flag on the compiler")]
        [Category("Compiler flags")]
        public bool TypeScriptUseAmdModule { get; set; }

        [LocDisplayName("Compile to EcmaScript 3")]
        [Description("Sets the '--target ES3' flag on the compiler. Default is EcmaScript 5")]
        [Category("Compiler flags")]
        public bool TypeScriptCompileES3 { get; set; }

        [LocDisplayName("Generate Source Map")]
        [Description("Sets the '-sourcemap' flag on the compiler.")]
        [Category("Compiler flags")]
        public bool TypeScriptProduceSourceMap { get; set; }

        [LocDisplayName("Keep comments")]
        [Description("Keeps the comments in the generated JavaScript files by setting the '-c' flag on the compiler.")]
        [Category("Compiler flags")]
        public bool TypeScriptKeepComments { get; set; }
    }
}
