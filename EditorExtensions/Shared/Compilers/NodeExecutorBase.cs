using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions
{
    public abstract class NodeExecutorBase
    {
        protected static readonly string WebEssentialsResourceDirectory = Path.Combine(Path.GetDirectoryName(typeof(NodeExecutorBase).Assembly.Location), @"Resources");
        private static readonly string NodePath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\node.exe");

        protected abstract string CompilerPath { get; }
        protected virtual Regex ErrorParsingPattern { get { return null; } }
        protected virtual Func<string, IEnumerable<CompilerError>> ParseErrors { get { return ParseErrorsWithRegex; } }

        ///<summary>Indicates whether this compiler will emit a source map file.  Will only return true if aupported and enabled in user settings.</summary>
        public abstract bool GenerateSourceMap { get; }
        public abstract string TargetExtension { get; }
        public abstract string ServiceName { get; }
        ///<summary>Indicates whether this compiler is capable of compiling to a filename that doesn't match the source filename.</summary>
        public virtual bool RequireMatchingFileName { get { return false; } }

        public async Task<CompilerResult> CompileAsync(string sourceFileName, string targetFileName)
        {
            if (RequireMatchingFileName &&
                Path.GetFileName(targetFileName) != Path.GetFileNameWithoutExtension(sourceFileName) + TargetExtension &&
                Path.GetFileName(targetFileName) != Path.GetFileNameWithoutExtension(sourceFileName) + ".min" + TargetExtension)
                throw new ArgumentException(ServiceName + " cannot compile to a targetFileName with a different name.  Only the containing directory can be different.", "targetFileName");

            var mapFileName = GetMapFileName(sourceFileName, targetFileName);

            var scriptArgs = GetArguments(sourceFileName, targetFileName, mapFileName);

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
                ProjectHelpers.CheckOutFileFromSourceControl(targetFileName);

                mapFileName = mapFileName ?? targetFileName + ".map";

                if (GenerateSourceMap)
                    ProjectHelpers.CheckOutFileFromSourceControl(mapFileName);

                using (var process = await start.ExecuteAsync())
                {
                    if (targetFileName != null)
                        await MoveOutputContentToCorrectTarget(targetFileName);

                    return await ProcessResult(
                                     process,
                                     (await FileHelpers.ReadAllTextRetry(errorOutputFile)).Trim(),
                                     sourceFileName,
                                     targetFileName,
                                     mapFileName
                                 );
                }
            }
            finally
            {
                File.Delete(errorOutputFile);

                if (!GenerateSourceMap)
                    File.Delete(mapFileName);
            }
        }

        private async Task<CompilerResult> ProcessResult(Process process, string errorText, string sourceFileName, string targetFileName, string mapFileName)
        {
            string result = "";
            bool success = false;
            IEnumerable<CompilerError> errors = null;

            try
            {
                if (process.ExitCode == 0)
                {
                    if (!string.IsNullOrEmpty(targetFileName) && File.Exists(targetFileName))
                        result = await FileHelpers.ReadAllTextRetry(targetFileName);

                    success = true;
                }
                else
                {
                    errors = ParseErrors(errorText);
                }
            }
            catch (FileNotFoundException missingFileException)
            {
                Logger.Log(ServiceName + ": " + Path.GetFileName(targetFileName) + " compilation failed. " + missingFileException.Message);
            }

            if (success)
            {
                var renewedResult = await PostProcessResult(result, sourceFileName, targetFileName, mapFileName);

                if (!ReferenceEquals(result, renewedResult))
                {
                    await FileHelpers.WriteAllTextRetry(targetFileName, renewedResult);
                    result = renewedResult;
                }
            }

            var compilerResult = await CompilerResultFactory.GenerateResult(
                                           sourceFileName: sourceFileName,
                                           targetFileName: targetFileName,
                                           mapFileName: mapFileName,
                                           isSuccess: success,
                                           result: result,
                                           errors: errors
                                       ) as CompilerResult;

            if (!success)
            {
                var firstError = compilerResult.Errors.Where(e => e != null).Select(e => e.Message).FirstOrDefault();

                if (firstError != null)
                    Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compilation failed: " + firstError);
            }

            return compilerResult;
        }

        protected IEnumerable<CompilerError> ParseErrorsWithJson(string error)
        {
            if (string.IsNullOrEmpty(error))
                return null;

            try
            {
                CompilerError[] results = JsonConvert.DeserializeObject<CompilerError[]>(error);

                if (results.Length == 0)
                    Logger.Log(ServiceName + " parse error: " + error);

                return results;
            }
            catch (JsonReaderException)
            {
                Logger.Log(ServiceName + " parse error: " + error);
                return new[] { new CompilerError() { Message = error } };
            }
        }

        protected IEnumerable<CompilerError> ParseErrorsWithRegex(string error)
        {
            var matches = ErrorParsingPattern.Matches(error);

            if (matches.Count == 0)
            {
                Logger.Log(ServiceName + ": unparsable compilation error: " + error);
                return new[] { new CompilerError { Message = error } };
            }
            return matches.Cast<Match>().Select(match => new CompilerError
            {
                FileName = match.Groups["fileName"].Value,
                Message = match.Groups["message"].Value,
                Column = string.IsNullOrEmpty(match.Groups["column"].Value) ? 1 : int.Parse(match.Groups["column"].Value, CultureInfo.CurrentCulture),
                Line = int.Parse(match.Groups["line"].Value, CultureInfo.CurrentCulture)
            });
        }

        /// <summary>
        ///  In case of CoffeeScript, the compiler doesn't take output file path argument,
        ///  instead takes path to output directory. This method can be overridden by any
        ///  such compiler to move data to correct target.
        /// </summary>
        protected virtual Task MoveOutputContentToCorrectTarget(string targetFileName)
        {
            return Task.FromResult(0);
        }

        protected virtual string GetMapFileName(string sourceFileName, string targetFileName)
        {
            return null;
        }

        protected abstract string GetArguments(string sourceFileName, string targetFileName, string mapFileName);

        protected abstract Task<string> PostProcessResult(string resultSource, string sourceFileName, string targetFileName, string mapFileName);
    }
}
