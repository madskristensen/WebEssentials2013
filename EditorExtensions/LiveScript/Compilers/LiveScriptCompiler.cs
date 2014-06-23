using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.LiveScript
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(LiveScriptContentTypeDefinition.LiveScriptContentType)]
    public class LiveScriptCompiler : JsCompilerBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\LiveScript\bin\livescript");
        private static readonly Regex _errorParsingPattern = new Regex(@"Failed at: (?<filename>.*?)Error: (?<message>.*)", RegexOptions.Multiline);

        public override bool GenerateSourceMap { get { return WESettings.Instance.LiveScript.GenerateSourceMaps; } }
        public override string ServiceName { get { return "LiveScript"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        public override bool RequireMatchingFileName { get { return true; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            var args = new StringBuilder();

            if (!WESettings.Instance.LiveScript.WrapClosure)
                args.Append("-b ");

            args.AppendFormat(CultureInfo.CurrentCulture, "-o \"{0}\" -c \"{1}\"", Path.GetDirectoryName(targetFileName), sourceFileName);
            return args.ToString();
        }

        protected async override Task<string> PostProcessResult(string resultSource, string sourceFileName, string targetFileName, string mapFileName)
        {
            Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");

            return await Task.FromResult(resultSource);
        }
    }
}