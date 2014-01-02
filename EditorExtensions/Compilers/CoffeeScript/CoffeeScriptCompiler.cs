using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    public class CoffeeScriptCompiler : NodeExecutorBase
    {
        private static readonly Regex _errorParsingPattern = new Regex(@".*\\(?<fileName>.*):(?<line>.\d*):(?<column>.\d*): error: (?<message>.*\n.*)", RegexOptions.Multiline);

        protected override string ServiceName
        {
            get { return "CoffeeScript"; }
        }
        protected override string CompilerPath
        {
            get { return @"node_modules\coffee-script\bin\coffee"; }
        }
        protected override Regex ErrorParsingPattern
        {
            get { return _errorParsingPattern; }
        }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            var args = new StringBuilder();
            if (WESettings.GetBoolean(WESettings.Keys.WrapCoffeeScriptClosure))
                args.Append("--bare ");

            if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptSourceMaps))
                args.Append("--map ");

            args.AppendFormat(CultureInfo.CurrentCulture, "--output \"{0}\" --compile \"{1}\"", Path.GetDirectoryName(targetFileName), sourceFileName);
            return args.ToString();
        }

        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            Logger.Log("CoffeeScript: " + Path.GetFileName(sourceFileName) + " compiled.");
            ProcessMapFile(targetFileName);

            return resultSource;
        }

        protected static void ProcessMapFile(string jsFileName)
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

            if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptSourceMaps))
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    MarginBase.AddFileToProject(jsFileName, sourceMapFile);
                }), DispatcherPriority.ApplicationIdle, null);
            }
        }
    }
}