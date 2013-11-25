using System;
using System.Diagnostics;
using System.IO;
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

        private static Task<Process> ExecuteAsync(ProcessStartInfo startInfo)
        {
            var p = Process.Start(startInfo);

            var tcs = new TaskCompletionSource<Process>();

            p.EnableRaisingEvents = true;
            p.Exited += (s, e) => tcs.TrySetResult(p);
            if (p.HasExited)
                tcs.TrySetResult(p);
            return tcs.Task;
        }

        public static async Task<CompilerResult> Compile(string filename, string targetFilename = null, string sourceMapRootPath = null)
        {
            string output = Path.GetTempFileName();

            string webEssentialsDir = Path.GetDirectoryName(typeof(LessCompiler).Assembly.Location);
            string lessc = Path.Combine(webEssentialsDir, @"Resources\nodejs\node_modules\.bin\lessc.cmd");
            string arguments = String.Format("--no-color --relative-urls \"{0}\" \"{1}\"", filename, output);
            string fileNameWithoutPath = Path.GetFileName(filename);
            string sourceMapArguments = (sourceMapRootPath != null) ?
                String.Format("--source-map-rootpath=\"{0}\" ", sourceMapRootPath.Replace("\\", "/")) : "";

            if (WESettings.GetBoolean(WESettings.Keys.LessSourceMaps))
                arguments = String.Format(
                  "--relative-urls {0}--source-map=\"{1}.map\" \"{2}\" \"{3}\"",
                  sourceMapArguments,
                  fileNameWithoutPath,
                  filename,
                  output);

            ProcessStartInfo start = new ProcessStartInfo(lessc)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.Combine(webEssentialsDir, @"Resources\nodejs"),
                CreateNoWindow = true,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using (var process = await ExecuteAsync(start))
            {
                CompilerResult result = new CompilerResult(filename);

                ProcessResult(output, process, result);

                if (result.IsSuccess)
                {
                    // Inserts an empty row between each rule and replace two space indentation with 4 space indentation
                    result.Result = _endingCurlyBraces.Replace(_linesStartingWithTwoSpaces.Replace(result.Result.Trim(), "$1$2"), "$&\n");

                    // If the caller wants us to renormalize URLs to a different filename, do so.
                    if (targetFilename != null && result.Result.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        try
                        {
                            result.Result = CssUrlNormalizer.NormalizeUrls(
                                tree: new CssParser().Parse(result.Result, true),
                                targetFile: targetFilename,
                                oldBasePath: filename
                            );
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("An error occurred while normalizing generated paths in " + filename + "\r\n" + ex);
                        }
                    }
                }
                Logger.Log(Path.GetFileName(filename) + " compiled");
                return result;
            }
        }

        private static void ProcessResult(string outputFile, Process process, CompilerResult result)
        {
            if (!File.Exists(outputFile))
                throw new FileNotFoundException("LESS compiled output not found", outputFile);

            if (process.ExitCode == 0)
            {
                result.Result = File.ReadAllText(outputFile);
                result.IsSuccess = true;
            }
            else
            {
                using (StreamReader reader = process.StandardError)
                {
                    string error = reader.ReadToEnd();
                    Debug.WriteLine("LessCompiler Error: " + error);
                    result.Error = ParseError(error);
                }
            }

            File.Delete(outputFile);
        }

        static readonly Regex errorParser = new Regex(@"^(.+) in (.+) on line (\d+), column (\d+):$", RegexOptions.Multiline);
        private static CompilerError ParseError(string error)
        {
            var m = errorParser.Match(error);
            if (!m.Success)
            {
                Logger.Log("Unparseable LESS error: " + error);
                return new CompilerError { Message = error };
            }
            return new CompilerError
            {
                Message = m.Groups[1].Value,
                FileName = m.Groups[2].Value,
                Line = int.Parse(m.Groups[3].Value),
                Column = int.Parse(m.Groups[4].Value)
            };
        }
    }
}