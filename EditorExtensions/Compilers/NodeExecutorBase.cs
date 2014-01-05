using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace MadsKristensen.EditorExtensions
{
    public abstract class NodeExecutorBase
    {
        protected static readonly string WebEssentialsResourceDirectory = Path.Combine(Path.GetDirectoryName(typeof(LessCompiler).Assembly.Location), @"Resources");
        private static readonly string NodePath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\node.exe");

        ///<summary>If set, the executor will not try to use the VS project system.</summary>
        public static bool InUnitTests { get; set; }

        protected abstract string ServiceName { get; }
        protected abstract string CompilerPath { get; }
        protected virtual Regex ErrorParsingPattern { get { return null; } }
        protected virtual Func<string, IEnumerable<CompilerError>> ParseErrors { get { return ParseErrorsWithRegex; } }

        public async Task<CompilerResult> Compile(string sourceFileName, string targetFileName)
        {
            var scriptArgs = GetArguments(sourceFileName, targetFileName);

            var errorOutputFile = Path.GetTempFileName();

            var cmdArgs = string.Format("\"{0}\" \"{1}\"", NodePath, CompilerPath);

            cmdArgs = string.Format("/c \"{0} {1} > \"{2}\" 2>&1\"", cmdArgs, scriptArgs, errorOutputFile);

            ProcessStartInfo start = new ProcessStartInfo("cmd")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetDirectoryName(sourceFileName),
                Arguments = cmdArgs,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = await start.ExecuteAsync())
                {
                    string errorText = "";

                    if (File.Exists(errorOutputFile) && process.ExitCode != 0)
                        errorText = File.ReadAllText(errorOutputFile);

                    return ProcessResult(process, errorText, sourceFileName, targetFileName);
                }
            }
            finally
            {
                File.Delete(errorOutputFile);
            }
        }

        private CompilerResult ProcessResult(Process process, string errorText, string sourceFileName, string targetFileName)
        {
            CompilerResult result = new CompilerResult(sourceFileName);

            ValidateResult(process, targetFileName, errorText, result);

            if (result.IsSuccess)
            {
                result.Result = PostProcessResult(result.Result, sourceFileName, targetFileName);
                if (!InUnitTests)
                    ProjectHelpers.AddFileToProject(sourceFileName, targetFileName);
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
                    result.Errors = ParseErrors(errorText.Replace("\r", ""));
                }
            }
            catch (FileNotFoundException missingFileException)
            {
                Logger.Log(ServiceName + ": " + Path.GetFileName(outputFile) + " compilation failed. " + missingFileException.Message);
            }
        }

        protected IEnumerable<CompilerError> ParseErrorsWithJson(string error)
        {
            if (string.IsNullOrEmpty(error))
                return null;

            try
            {
                CompilerError[] results = Json.Decode<CompilerError[]>(error);

                if (results.Length == 0)
                    Logger.Log(ServiceName + " parse error: " + error);

                return results;
            }
            catch (ArgumentException)
            {
                Logger.Log(ServiceName + " parse error: " + error);
                return new[] { new CompilerError() { Message = error } };
            }
        }

        protected IEnumerable<CompilerError> ParseErrorsWithRegex(string error)
        {
            var match = ErrorParsingPattern.Match(error);

            if (!match.Success)
            {
                Logger.Log(ServiceName + " parse error: " + error);
                yield return new CompilerError { Message = error };
            }
            yield return new CompilerError
            {
                FileName = match.Groups["fileName"].Value,
                Message = match.Groups["message"].Value,
                Column = int.Parse(match.Groups["column"].Value, CultureInfo.CurrentCulture),
                Line = int.Parse(match.Groups["line"].Value, CultureInfo.CurrentCulture)
            };
        }

        protected abstract string GetArguments(string sourceFileName, string targetFileName);

        protected abstract string PostProcessResult(string resultSource, string sourceFileName, string targetFileName);
    }
}
