using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public static class LessCompiler
    {
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
            string argumentFormat = "--relative-urls \"{0}\" \"{1}\"";
            if (WESettings.GetBoolean(WESettings.Keys.LessSourceMaps))
              argumentFormat = "--relative-urls --line-numbers=all --source-map \"{0}\" \"{1}\"";

            ProcessStartInfo start = new ProcessStartInfo(lessc)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                Arguments = String.Format(argumentFormat, filename, output),
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using (var process = await ExecuteAsync(start))
            {
                CompilerResult result = new CompilerResult(filename);

                ProcessResult(output, process, result);

                // If the caller wants us to renormalize URLs to a different filename, do so.
                if (targetFilename != null && result.IsSuccess && result.Result.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
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