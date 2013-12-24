using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public abstract class NodeExecutorBase
    {
        protected static readonly string WebEssentialsNodeDirectory = Path.Combine(Path.GetDirectoryName(typeof(LessCompiler).Assembly.Location), @"Resources\nodejs");
        protected static readonly string Node = Path.Combine(WebEssentialsNodeDirectory, @"node.exe");

        protected abstract string Compiler { get; }

        public abstract Task<CompilerResult> RunCompile(string fileName, string targetFileName);

        protected virtual async Task<CompilerResult> Compile(string fileName, string targetFileName, string arguments, string output = null)
        {
            ProcessStartInfo start = new ProcessStartInfo(String.Format("\"{0}\" \"{1}\"", (File.Exists(Node)) ? Node : "node", Path.Combine(WebEssentialsNodeDirectory, Compiler)))
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

            using (var process = await start.ExecuteAsync(error))
            {
                return ProcessResult(process, error.ToString(), fileName, targetFileName, output);
            }
        }

        protected abstract CompilerResult ProcessResult(Process process, string errorText, string fileName, string targetFileName, string output);
    }
}
