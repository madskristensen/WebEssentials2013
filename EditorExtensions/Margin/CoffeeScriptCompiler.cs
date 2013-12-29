using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    public class CoffeeScriptCompiler : NodeExecutorBase
    {
        private static readonly Regex _errorParsingPattern = new Regex(@".*\\(?<fileName>.*):(?<line>.\d):(?<column>.\d): error: (?<message>.*\n.*)", RegexOptions.Multiline);

        protected override string ServiceName
        {
            get { return "CoffeeScript"; }
        }
        protected override string CompilerPath
        {
            get { return @"node_modules\iced-coffee-script\bin\coffee"; }
        }
        protected override Regex ErrorParsingPattern
        {
            get { return _errorParsingPattern; }
        }

        protected override void SetArguments(string sourceFileName, string targetFileName)
        {
            Arguments = WESettings.GetBoolean(WESettings.Keys.WrapCoffeeScriptClosure) ? "--bare " : "";

            if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptSourceMaps))
            {
                Arguments += String.Format(CultureInfo.CurrentCulture, "--runtime inline --output \"{0}\" --map --compile \"{1}\"", Path.GetDirectoryName(targetFileName), sourceFileName);
                return;
            }

            Arguments += String.Format(CultureInfo.CurrentCulture, "--runtime inline --output \"{0}\" --compile \"{1}\"", Path.GetDirectoryName(targetFileName), sourceFileName);
        }

        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            Logger.Log("CoffeeScript: " + Path.GetFileName(sourceFileName) + " compiled.");

            return resultSource;
        }
    }
}