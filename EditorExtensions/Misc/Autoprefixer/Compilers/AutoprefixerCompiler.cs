using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Settings;

namespace MadsKristensen.EditorExtensions.Autoprefixer
{
    [Export(typeof(NodeExecutorBase))]
    public class AutoprefixerCompiler : NodeExecutorBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\autoprefixer\bin\autoprefixer");
        private static readonly Regex _errorParsingPattern = new Regex(@"(?<fileName>.*):(?<line>.\d*): error: (?<message>.*\n.*)", RegexOptions.Multiline);

        public override string ServiceName { get { return "Autoprefixer"; } }
        public override string TargetExtension { get { return ".autoprefix"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }
        public override bool GenerateSourceMap { get { return false; } }
        public override bool ManagedSourceMap { get { return false; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            // Source maps would be generated in "ALL" cases (regardless of the settings).

            var browsers = string.Empty;
            if (!string.IsNullOrWhiteSpace(WESettings.Instance.Css.AutoprefixerBrowsers))
            {
                browsers = "--browsers \"" + WESettings.Instance.Css.AutoprefixerBrowsers.Replace("\\", "\\\\").Replace("\"", "'") + "\"";
            }
            return string.Format(CultureInfo.CurrentCulture, "\"{0}\" --map {1}", sourceFileName, browsers);
        }

        protected override Task<string> PostProcessResult(string resultSource, string sourceFileName, string targetFileName, string mapFileName)
        {
            Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");
            return Task.FromResult(resultSource);
        }
    }
}
