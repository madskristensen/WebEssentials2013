using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    public static class LessCompiler
    {
        private static readonly Regex _endingCurlyBraces = new Regex(@"}\W*}|}", RegexOptions.Compiled);
        private static readonly Regex _lineStrartsWithTwoSpaces = new Regex("^  ", RegexOptions.Multiline | RegexOptions.Compiled);

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

        public static async Task<CompilerResult> Compile(string filename, string targetFilename = null)
        {
            string output = Path.GetTempFileName();

            string webEssentialsDir = Path.GetDirectoryName(typeof(LessCompiler).Assembly.Location);
            string lessc = Path.Combine(webEssentialsDir, @"Resources\nodejs\node_modules\.bin\lessc.cmd");
            string arguments = String.Format("--relative-urls \"{0}\" \"{1}\"", filename, output);
            if (WESettings.GetBoolean(WESettings.Keys.LessSourceMaps))
                arguments = String.Format(
                  "--relative-urls --source-map=\"{0}.map\" \"{1}\" \"{2}\"",
                  targetFilename ?? filename,
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
                    result.Result = _endingCurlyBraces.Replace(_lineStrartsWithTwoSpaces.Replace(result.Result.Trim(), "  $&"), "$&\n");

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

        private static CompilerError ParseError(string error)
        {
            CompilerError result = new CompilerError();
            string[] lines = error.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (error.Contains("message:"))
                {
                    string[] args = line.Split(new[] { ':' }, 2);

                    if (args[0].Trim() == "message")
                        result.Message = args[1].Trim();

                    if (args[0].Trim() == "filename")
                        result.FileName = args[1].Trim();

                    int lineNo = 0;
                    if (args[0].Trim() == "line" && int.TryParse(args[1], out lineNo))
                        result.Line = lineNo;

                    int columnNo = 0;
                    if (args[0].Trim() == "column" && int.TryParse(args[1], out columnNo))
                        result.Column = columnNo;
                }
                else
                {
                    if (i == 1 || i == 2)
                        result.Message += " " + line;

                    if (i == 3)
                    {
                        string[] lineCol = line.Split(',');

                        int lineNo = 0;
                        if (int.TryParse(lineCol[0].Replace("on line", string.Empty).Trim(), out lineNo))
                            result.Line = lineNo;

                        int columnNo = 0;
                        if (int.TryParse(lineCol[0].Replace("column", string.Empty).Trim(':').Trim(), out columnNo))
                            result.Column = columnNo;

                        result.Message = result.Message.Trim();
                    }

                }
            }

            return result;
        }
    }
}