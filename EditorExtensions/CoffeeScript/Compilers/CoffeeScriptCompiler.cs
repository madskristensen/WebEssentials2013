using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.CoffeeScript
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType("CoffeeScript")]
    public class CoffeeScriptCompiler : NodeExecutorBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\coffee-script\bin\coffee");
        private static readonly Regex _errorParsingPattern = new Regex(@"(?<fileName>.*):(?<line>.\d*):(?<column>.\d*): error: (?<message>.*\n.*)", RegexOptions.Multiline);
        private static readonly Regex _sourceMapInJs = new Regex(@"\/\/\\*#([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*", RegexOptions.Multiline);

        public override string TargetExtension { get { return ".js"; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.CoffeeScript.GenerateSourceMaps && !WESettings.Instance.CoffeeScript.MinifyInPlace; } }
        public override string ServiceName { get { return "CoffeeScript"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        public override bool RequireMatchingFileName { get { return true; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            var args = new StringBuilder();

            if (!WESettings.Instance.CoffeeScript.WrapClosure)
                args.Append("--bare ");

            if (GenerateSourceMap)
                args.Append("--map ");

            args.AppendFormat(CultureInfo.CurrentCulture, "--output \"{0}\" --compile \"{1}\"", Path.GetDirectoryName(targetFileName), sourceFileName);
            return args.ToString();
        }

        protected async override Task MoveOutputContentToCorrectTarget(string targetFileName)
        {
            if (!targetFileName.EndsWith(".min.js", System.StringComparison.OrdinalIgnoreCase))
                return;

            var tempName = targetFileName.Replace(".min.js", ".js");

            if (!File.Exists(tempName))
                return;

            await FileHelpers.WriteAllTextRetry(targetFileName, await FileHelpers.ReadAllTextRetry(tempName));
        }

        protected async override Task<string> PostProcessResult(string resultSource, string sourceFileName, string targetFileName, string mapFileName)
        {
            Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");

            string realTargetFileName = Path.Combine(Path.GetDirectoryName(targetFileName), FileHelpers.GetFileNameWithoutExtension(targetFileName) + ".js");

            if (WESettings.Instance.CoffeeScript.MinifyInPlace)
            {
                File.Delete(realTargetFileName); // Because CoffeeScript compiler doesn't take custom file name as parameter.
            }

            if (GenerateSourceMap)
            {
                if (File.Exists(mapFileName))
                    File.Delete(mapFileName);

                File.Move(Path.ChangeExtension(realTargetFileName, ".map"), mapFileName);

                resultSource = UpdateSourceLinkInJsComment(resultSource, FileHelpers.RelativePath(targetFileName, mapFileName));
            }

            return await Task.FromResult(resultSource);
        }

        private static string UpdateSourceLinkInJsComment(string content, string sourceMapRelativePath)
        {
            return _sourceMapInJs.Replace(content,
                   string.Format(CultureInfo.InvariantCulture,
                   "//# sourceMappingURL={0}", sourceMapRelativePath));
        }
    }
}