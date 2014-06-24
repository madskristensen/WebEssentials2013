using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private static readonly Regex _errorParsingPattern = new Regex(@"\A(?<fileName>.+):(?<line>.\d+): error: (?<fullMessage>(?<message>.*)\z)", RegexOptions.Multiline | RegexOptions.Compiled);

        public override string ServiceName { get { return "SCSS"; } }
        public override string TargetExtension { get { return ".css"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.Scss.GenerateSourceMaps && !WESettings.Instance.Scss.MinifyInPlace; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            string outputStyle = WESettings.Instance.Scss.OutputStyle.ToString().ToLowerInvariant();
            string numberPrecision = WESettings.Instance.Scss.NumberPrecision.ToString(CultureInfo.CurrentCulture).ToLowerInvariant();

            // Source maps would be generated in "ALL" cases (regardless of the settings).
            // If the option in settings is disabled, we will delete the map file once the
            // B64VLQ values are extracted.
            return string.Format(CultureInfo.CurrentCulture,
                   "--source-map \"{0}\" --output-style={1} \"{2}\" --output \"{3}\" --precision={4}",
                   mapFileName,
                   outputStyle,
                   sourceFileName,
                   targetFileName,
                   numberPrecision);
        }

        //https://github.com/hcatlin/libsass/issues/242
        protected async override Task<string> ReadMapFile(string sourceMapFileName)
        {
            return (await FileHelpers.ReadAllTextRetry(sourceMapFileName)).Replace("\\", "\\\\");
        }
    }
}