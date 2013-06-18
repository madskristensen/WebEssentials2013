using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MadsKristensen.EditorExtensions
{
    public class LessCompiler
    {
        public LessCompiler(Action<CompilerResult> callback)
        {
            Callback = callback;
        }

        public Action<CompilerResult> Callback { get; set; }

        public void Compile(string fileName)
        {
            string output = Path.GetTempFileName();

            ProcessStartInfo start = new ProcessStartInfo(@"cscript");
            start.WindowStyle = ProcessWindowStyle.Hidden;
            start.CreateNoWindow = true;
            start.Arguments = "//nologo //s \"" + GetExecutablePath() + "\" \"" + fileName + "\" \"" + output + "\"";
            start.EnvironmentVariables["output"] = output;
            start.EnvironmentVariables["fileName"] = fileName;
            start.UseShellExecute = false;
            start.RedirectStandardError = true;

            Process p = new Process();
            p.StartInfo = start;
            p.EnableRaisingEvents = true;
            p.Exited += ProcessExited;
            p.Start();
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            using (Process process = (Process)sender)
            {
                string fileName = process.StartInfo.EnvironmentVariables["fileName"];
                CompilerResult result = new CompilerResult(fileName);

                try
                {
                    ProcessResult(process, result);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    Callback(result);
                }

                process.Exited -= ProcessExited;

                Logger.Log(Path.GetFileName(fileName) + " compiled");
            }
        }

        private void ProcessResult(Process process, CompilerResult result)
        {
            string output = process.StartInfo.EnvironmentVariables["output"];

            if (File.Exists(output))
            {
                if (process.ExitCode == 0)
                {
                    result.IsSuccess = true;
                    result.Result = File.ReadAllText(output);
                }
                else
                {
                    using (StreamReader reader = process.StandardError)
                    {
                        result.Error = ParseError(reader.ReadToEnd());
                    }
                }

                File.Delete(output);
            }

            Callback(result);
        }

        private CompilerError ParseError(string error)
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

        private static string GetExecutablePath()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly).ToLowerInvariant();
            string file = Path.Combine(folder, "resources\\scripts\\lessc.wsf");

            return file;
        }
    }
}