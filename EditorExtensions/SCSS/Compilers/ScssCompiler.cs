using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Scss
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(ScssContentTypeDefinition.ScssContentType)]
    public class ScssCompiler : CssCompilerBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\node-sass\bin\node-sass");
        private static readonly Regex _errorParsingPattern = new Regex(@"(?<fileName>.*):(?<line>.\d*): error: (?<message>.*\n.*)", RegexOptions.Multiline);

        public override string ServiceName { get { return "SCSS"; } }
        public override string TargetExtension { get { return ".css"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.Scss.GenerateSourceMaps; } }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            MapFileName = targetFileName + ".map";
            MapFileName = GenerateSourceMap ? MapFileName : Path.Combine(Path.GetTempPath(), Path.GetFileName(MapFileName));

            return string.Format(CultureInfo.CurrentCulture, "--source-map \"{0}\" --output-style=expanded \"{1}\" --output \"{2}\"", MapFileName, sourceFileName, targetFileName);
        }

        //https://github.com/hcatlin/libsass/issues/242
        protected override string ReadMapFile(string sourceMapFileName)
        {
            return File.ReadAllText(sourceMapFileName).Replace("\\", "\\\\");
        }
    }
}