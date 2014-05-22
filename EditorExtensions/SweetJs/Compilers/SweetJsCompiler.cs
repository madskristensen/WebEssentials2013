using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.SweetJs
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    public class SweetJsCompiler : NodeExecutorBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\sweet.js\bin\sjs");
        private static readonly Regex _errorParsingPattern = new Regex(@"(?<fileName>.*):(?<line>.\d*):(?<column>.\d*): error: (?<message>.*\n.*)", RegexOptions.Multiline);

        public override string TargetExtension { get { return ".js"; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.SweetJs.GenerateSourceMaps && !WESettings.Instance.SweetJs.MinifyInPlace; } }
        public override string ServiceName { get { return SweetJsContentTypeDefinition.SweetJsContentType; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        public override bool RequireMatchingFileName { get { return false; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            var args = new StringBuilder();

            if (GenerateSourceMap)
                args.Append("--sourcemap ");

            args.AppendFormat(CultureInfo.CurrentCulture, "--output \"{0}\" \"{1}\"", targetFileName, sourceFileName);
            return args.ToString();
        }

        protected async override Task<string> PostProcessResult(string resultSource, string sourceFileName, string targetFileName, string mapFileName)
        {
            Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");

            return await Task.FromResult(resultSource);
        }
    }
}
