using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType("CoffeeScript")]
    public class CoffeeScriptCompiler : NodeExecutorBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\coffee-script\bin\coffee");
        private static readonly Regex _errorParsingPattern = new Regex(@"(?<fileName>.*):(?<line>.\d*):(?<column>.\d*): error: (?<message>.*\n.*)", RegexOptions.Multiline);
        private static readonly Regex _sourceMapInJs = new Regex(@"\/\*\n.*=(.*)\n\*\/", RegexOptions.Multiline);

        public override string TargetExtension { get { return ".js"; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.CoffeeScript.GenerateSourceMaps; } }
        public override string ServiceName { get { return "CoffeeScript"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            var args = new StringBuilder();

            if (!WESettings.Instance.CoffeeScript.WrapClosure)
                args.Append("--bare ");

            if (GenerateSourceMap)
                args.Append("--map ");

            args.AppendFormat(CultureInfo.CurrentCulture, "--output \"{0}\" --compile \"{1}\"", Path.GetDirectoryName(targetFileName), sourceFileName);
            return args.ToString();
        }

        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");
            ProcessMapFile(targetFileName);

            return UpdateSourceMapUrls(resultSource, targetFileName);
        }

        private static void ProcessMapFile(string jsFileName)
        {
            var sourceMapFile = jsFileName + ".map";

            /*if (!File.Exists(sourceMapFile)) // To be uncommented when following issue is resolved.
                return;*/

            // Hack: Remove if / when this issue is resolved: https://github.com/jashkenas/coffee-script/issues/3297
            var oldSourceMapFile = Path.ChangeExtension(jsFileName, ".map");

            if (!File.Exists(oldSourceMapFile))
                return;

            File.Copy(oldSourceMapFile, sourceMapFile, true);
            File.Delete(oldSourceMapFile);
            // end-Hack
        }

        private static string UpdateSourceMapUrls(string content, string compiledFileName)
        {
            if (!WESettings.Instance.CoffeeScript.GenerateSourceMaps || !File.Exists(compiledFileName))
                return content;

            string sourceMapFilename = compiledFileName + ".map";

            if (!File.Exists(sourceMapFilename))
                return content;


            string sourceMapRelativePath = FileHelpers.RelativePath(compiledFileName, sourceMapFilename);

            // Fix sourceMappingURL comment in JS file with network accessible path.
            return _sourceMapInJs.Replace(content,
                string.Format(CultureInfo.CurrentCulture, @"/*{1}//@ sourceMappingURL={0}{1}*/", sourceMapRelativePath, Environment.NewLine));
        }
    }
}