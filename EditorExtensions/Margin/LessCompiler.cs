using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public static class LessCompiler
    {
        private static readonly Regex _endingCurlyBraces = new Regex(@"}\W*}|}", RegexOptions.Compiled);
        private static readonly Regex _linesStartingWithTwoSpaces = new Regex("(\n( *))", RegexOptions.Compiled);
        private static readonly string webEssentialsNodeDir = Path.Combine(Path.GetDirectoryName(typeof(LessCompiler).Assembly.Location), @"Resources\nodejs");
        private static readonly string lessCompiler = Path.Combine(webEssentialsNodeDir, @"node_modules\less\bin\lessc");
        private static readonly string node = Path.Combine(webEssentialsNodeDir, @"node.exe");
        private static readonly Regex errorParser = new Regex(@"^(.+) in (.+) on line (\d+), column (\d+):$", RegexOptions.Multiline);

        public static async Task<CompilerResult> Compile(string fileName, string targetFileName = null, string sourceMapRootPath = null)
        {
            string output = Path.GetTempFileName();
            string arguments = String.Format("--no-color --relative-urls \"{0}\" \"{1}\"", fileName, output);
            string fileNameWithoutPath = Path.GetFileName(fileName);
            string sourceMapArguments = (string.IsNullOrEmpty(sourceMapRootPath)) ? "" : String.Format("--source-map-rootpath=\"{0}\" ", sourceMapRootPath.Replace("\\", "/"));

            if (WESettings.GetBoolean(WESettings.Keys.LessSourceMaps))
                arguments = String.Format("--no-color --relative-urls {0}--source-map=\"{1}.map\" \"{2}\" \"{3}\"", sourceMapArguments, fileNameWithoutPath, fileName, output);

            ProcessStartInfo start = new ProcessStartInfo(String.Format("\"{0}\" \"{1}\"", (File.Exists(node)) ? node : "node", lessCompiler))
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetDirectoryName(fileName),
                CreateNoWindow = true,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var error = new StringBuilder();
            using (var process = await ExecuteAsync(start, error))
            {
                return ProcessResult(output, process, error.ToString(), fileName, targetFileName);
            }
        }

        private static Task<Process> ExecuteAsync(ProcessStartInfo startInfo, StringBuilder error)
        {
            var process = Process.Start(startInfo);
            var processTaskCompletionSource = new TaskCompletionSource<Process>();

            //note: if we don't also read from the standard output, we don't receive the error output... ?
            process.OutputDataReceived += (_, __) => { };
            process.ErrorDataReceived += (sender, line) =>
            {
                error.AppendLine(line.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.EnableRaisingEvents = true;
            EventHandler exitHandler = (s, e) =>
            {
                // WaitForExit() ensures that the StandardError stream has been drained
                process.WaitForExit();
                processTaskCompletionSource.TrySetResult(process);
            };

            process.Exited += exitHandler;

            if (process.HasExited) exitHandler(process, null);
            return processTaskCompletionSource.Task;
        }

        private static CompilerResult ProcessResult(string output, Process process, string errorText, string fileName, string targetFileName)
        {
            CompilerResult result = new CompilerResult(fileName);

            ProcessResult(output, process, errorText, result);

            if (result.IsSuccess)
            {
                // Inserts an empty row between each rule and replace two space indentation with 4 space indentation
                result.Result = _endingCurlyBraces.Replace(_linesStartingWithTwoSpaces.Replace(result.Result.Trim(), "$1$2"), "$&\n");

                var message = Path.GetFileName(fileName) + " compiled.";

                // If the caller wants us to renormalize URLs to a different filename, do so.
                if (targetFileName != null && result.Result.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    try
                    {
                        result.Result = CssUrlNormalizer.NormalizeUrls(
                            tree: new CssParser().Parse(result.Result, true),
                            targetFile: targetFileName,
                            oldBasePath: fileName
                        );
                    }
                    catch (Exception ex)
                    {
                        message = "An error occurred while normalizing generated paths in " + fileName + "\r\n" + ex;
                    }
                }

                Logger.Log(message);
            }
            else
            {
                Logger.Log(Path.GetFileName(fileName) + " compilation failed.");
            }

            return result;
        }

        private static void ProcessResult(string outputFile, Process process, string errorText, CompilerResult result)
        {
            if (!File.Exists(outputFile))
            {
                throw new FileNotFoundException("LESS compiled output not found", outputFile);
            }

            if (process.ExitCode == 0)
            {
                result.Result = File.ReadAllText(outputFile);
                result.IsSuccess = true;
            }
            else
            {
                result.Error = ParseError(errorText.Replace("\r", ""));
            }

            File.Delete(outputFile);
        }

        private static CompilerError ParseError(string error)
        {
            var match = errorParser.Match(error);

            if (!match.Success)
            {
                Logger.Log("LESS parse error: " + error);
                return new CompilerError { Message = error };
            }
            return new CompilerError
            {
                Message = match.Groups[1].Value,
                FileName = match.Groups[2].Value,
                Line = int.Parse(match.Groups[3].Value, CultureInfo.CurrentCulture),
                Column = int.Parse(match.Groups[4].Value, CultureInfo.CurrentCulture)
            };
        }
    }
}