using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    internal class CoffeeScriptCompiler : NodeExecutorBase
    {
        private static readonly Regex errorParser = new Regex(@".*\\(.*):(.\d):(.\d): error: (.*\n.*)", RegexOptions.Multiline);

        protected override string Compiler
        {
            get { return @"node_modules\iced-coffee-script\bin\coffee"; }
        }

        public override async Task<CompilerResult> RunCompile(string fileName, string targetFileName = null)
        {
            string baseFolder = ProjectHelpers.GetRootFolder() ?? Path.GetDirectoryName(targetFileName);
            string arguments = WESettings.GetBoolean(WESettings.Keys.WrapCoffeeScriptClosure) ?
                                                       "--bare " : "";

            arguments += String.Format("--runtime inline --output \"{0}\" --compile \"{1}\"", Path.GetDirectoryName(targetFileName), fileName);

            return await base.Compile(fileName, targetFileName, arguments);
        }

        protected override CompilerResult ProcessResult(Process process, string errorText, string fileName, string targetFileName, string output = null)
        {
            CompilerResult result = new CompilerResult(fileName);

            ProcessResult(targetFileName, process, errorText, result);

            if (result.IsSuccess)
            {
                Logger.Log("CoffeeScript: " + Path.GetFileName(fileName) + " compiled.");
            }
            else
            {
                Logger.Log("CoffeeScript: " + Path.GetFileName(fileName) + " compilation failed.");
                Logger.Log("CoffeeScript: Error on line " + result.Error.Line + " at position " + result.Error.Column + ": " + result.Error.Message);
            }

            return result;
        }

        private static void ProcessResult(string outputFile, Process process, string errorText, CompilerResult result)
        {
            try
            {
                if (process.ExitCode == 0)
                {
                    result.Result = File.ReadAllText(outputFile);
                    result.IsSuccess = true;
                }
                else
                {
                    result.Error = ParseError(errorText.Replace("\r", ""));
                }
            }
            catch (FileNotFoundException missingFileException)
            {
                Logger.Log("CoffeeScript: " + Path.GetFileName(outputFile) + " compilation failed. " + missingFileException.Message);
            }
        }

        private static CompilerError ParseError(string error)
        {
            var match = errorParser.Match(error);

            if (!match.Success)
            {
                Logger.Log("CoffeeScript parse error: " + error);
                return new CompilerError { Message = error };
            }
            return new CompilerError
            {
                FileName = match.Groups[1].Value,
                Line = int.Parse(match.Groups[2].Value, CultureInfo.CurrentCulture),
                Column = int.Parse(match.Groups[3].Value, CultureInfo.CurrentCulture),
                Message = match.Groups[4].Value
            };
        }
    }
}