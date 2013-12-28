using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public abstract class NodeExecutorBase
    {
        private string ErrorFileName;
        protected static readonly string WebEssentialsNodeDirectory = Path.Combine(Path.GetDirectoryName(typeof(LessCompiler).Assembly.Location), @"Resources\nodejs");
        protected static readonly string NodePath = (File.Exists(Path.Combine(WebEssentialsNodeDirectory, @"node.exe"))) ? Path.Combine(WebEssentialsNodeDirectory, @"node.exe") : "node";

        protected string Arguments { get; set; }
        protected abstract string ServiceName { get; }
        protected abstract string CompilerPath { get; }
        protected abstract Regex ErrorParsingPattern { get; }

        public async Task<CompilerResult> Compile(string sourceFileName, string targetFileName)
        {
            SetArguments(sourceFileName, targetFileName);

            ErrorFileName = Path.Combine(WebEssentialsNodeDirectory, String.Format(CultureInfo.CurrentCulture, "error-{0}.log", Guid.NewGuid().ToString()));

            var resultantArguments = string.Format("\"{0}\" \"{1}\"", NodePath, Path.Combine(WebEssentialsNodeDirectory, CompilerPath));

            resultantArguments = string.Format("/c \"{0} {1} > \"{2}\" 2>&1\"", resultantArguments, Arguments, ErrorFileName);

            ProcessStartInfo start = new ProcessStartInfo("cmd")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetDirectoryName(sourceFileName),
                Arguments = resultantArguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = await start.ExecuteAsync())
            {
                string errorText = "";

                if (File.Exists(ErrorFileName))
                {
                    errorText = File.ReadAllText(ErrorFileName);

                    File.Delete(ErrorFileName);
                }

                return ProcessResult(process, errorText, sourceFileName, targetFileName);
            }
        }

        private CompilerResult ProcessResult(Process process, string errorText, string sourceFileName, string targetFileName)
        {
            CompilerResult result = new CompilerResult(sourceFileName);

            ValidateResult(process, targetFileName, errorText, result);

            if (result.IsSuccess)
            {
                result.Result = PostProcessResult(result.Result, sourceFileName, targetFileName);
            }
            else
            {
                Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compilation failed.");
            }

            return result;
        }

        private void ValidateResult(Process process, string outputFile, string errorText, CompilerResult result)
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
                Logger.Log(ServiceName + ": " + Path.GetFileName(outputFile) + " compilation failed. " + missingFileException.Message);
            }
        }

        private CompilerError ParseError(string error)
        {
            var match = ErrorParsingPattern.Match(error);

            if (!match.Success)
            {
                Logger.Log(ServiceName + " parse error: " + error);
                return new CompilerError { Message = error };
            }
            return new CompilerError
            {
                FileName = match.Groups["fileName"].Value,
                Message = match.Groups["message"].Value,
                Column = int.Parse(match.Groups["column"].Value, CultureInfo.CurrentCulture),
                Line = int.Parse(match.Groups["line"].Value, CultureInfo.CurrentCulture)
            };
        }

        protected abstract void SetArguments(string sourceFileName, string targetFileName);

        protected abstract string PostProcessResult(string resultMessage, string sourceFileName, string targetFileName);
    }
}
