using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.Build.Framework;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// This is not compiled (ItemType=None) but is invoked by the Inline Task (http://msdn.microsoft.com/en-us/library/dd722601) in the csproj file.
    /// </summary>
    public class PreBuildTask : Microsoft.Build.Utilities.Task
    {
        public override bool Execute()
        {
            Directory.CreateDirectory(@"resources\nodejs\tools");
            // Force npm to install modules to the subdirectory
            // https://npmjs.org/doc/files/npm-folders.html#More-Information

            // We install our modules in this subdirectory so that
            // we can clean up their dependencies without catching
            // npm's modules, which we don't want.
            File.WriteAllText(@"resources\nodejs\tools\package.json", "{}");

            // Since this is a synchronous job, I have
            // no choice but to synchronously wait for
            // the tasks to finish. However, the async
            // still saves threads.

            Task.WaitAll(
                DownloadNodeAsync(),
                DownloadNpmAsync()
            );

            var moduleResults = Task.WhenAll(
                InstallModuleAsync("jscs", "jscs"),
                InstallModuleAsync("lessc", "less"),
                InstallModuleAsync("jshint", "jshint"),
                InstallModuleAsync("tslint", "tslint"),
                InstallModuleAsync("node-sass", "node-sass"),
                InstallModuleAsync("coffee", "coffee-script"),
                InstallModuleAsync("autoprefixer", "autoprefixer"),
                InstallModuleAsync("iced", "iced-coffee-script"),
                InstallModuleAsync("LiveScript", "LiveScript"),
                InstallModuleAsync("coffeelint", "coffeelint"),
                InstallModuleAsync("sjs", "sweet.js")
            ).Result.Where(r => r != ModuleInstallResult.AlreadyPresent);

            if (moduleResults.Contains(ModuleInstallResult.Error))
                return false;

            if (!moduleResults.Any())
                return true;

            Log.LogMessage(MessageImportance.High, "Installed " + moduleResults.Count() + " modules.  Flattening...");

            if (!FlattenModulesAsync().Result)
                return false;

            return true;
        }

        Task DownloadNodeAsync()
        {
            if (File.Exists(@"resources\nodejs\node.exe"))
                return Task.FromResult<object>(null);
            Log.LogMessage(MessageImportance.High, "Downloading nodejs ...");
            return new WebClient().DownloadFileTaskAsync("http://nodejs.org/dist/latest/node.exe", @"resources\nodejs\node.exe");
        }

        async Task DownloadNpmAsync()
        {
            if (File.Exists(@"resources\nodejs\node_modules\npm\bin\npm.cmd"))
                return;

            Log.LogMessage(MessageImportance.High, "Downloading npm ...");

            var npmZip = await new WebClient().OpenReadTaskAsync("http://nodejs.org/dist/npm/npm-1.3.23.zip");

            try
            {
                ExtractZipWithOverwrite(npmZip, @"resources\nodejs");
            }
            catch
            {
                // Make sure the next build doesn't see a half-installed npm
                Directory.Delete(@"resources\nodejs\node_modules\npm", true);
                throw;
            }
        }

        enum ModuleInstallResult { AlreadyPresent, Installed, Error }

        async Task<ModuleInstallResult> InstallModuleAsync(string cmdName, string moduleName)
        {
            if (File.Exists(@"resources\nodejs\tools\node_modules\.bin\" + cmdName + ".cmd"))
                return ModuleInstallResult.AlreadyPresent;

            Log.LogMessage(MessageImportance.High, "npm install " + moduleName + " ...");

            var output = await ExecWithOutputAsync(@"cmd", @"/c ..\npm.cmd install " + moduleName, @"resources\nodejs\tools");

            if (output != null)
            {
                Log.LogError("npm install " + moduleName + " error: " + output);
                return ModuleInstallResult.Error;
            }

            return ModuleInstallResult.Installed;
        }

        async Task<bool> FlattenModulesAsync()
        {
            var output = await ExecWithOutputAsync(@"cmd", @"/c ..\npm.cmd dedup ", @"resources\nodejs\tools");

            if (output != null)
            {
                Log.LogError("npm dedup error: " + output);

                return false;
            }

            FlattenNodeModules(@"resources\nodejs\tools");

            // Fix node-sass-middleware require:
            FixRequired();

            return true;
        }

        private void FixRequired()
        {
            string requiredFile = @"resources\nodejs\tools\node_modules\node-sass-middleware\middleware.js";
            string text = File.ReadAllText(requiredFile);

            text = Regex.Replace(text, @"require\(\'node-sass\'\)", @"require('../node-sass/bin/node-sass')");

            File.WriteAllText(requiredFile, text);
        }


        /// <summary>
        /// Due to the way node_modues work, the directory depth can get very deep and go beyond MAX_PATH (260 chars). 
        /// Therefore grab all node_modues directories and move them up to baseNodeModuleDir. Node's require() will then 
        /// traverse up and find them at the higher level. Should be fine as long as there are no versioning conflicts.
        /// </summary>
        void FlattenNodeModules(string baseNodeModuleDir)
        {
            var baseDir = new DirectoryInfo(baseNodeModuleDir);

            var nodeModulesDirs = from dir in baseDir.EnumerateDirectories("*", SearchOption.AllDirectories)
                                  where dir.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                                  orderby dir.FullName.Count(c => c == Path.DirectorySeparatorChar) descending // Get deepest first
                                  select dir;

            foreach (var nodeModules in nodeModulesDirs)
            {
                foreach (var module in nodeModules.EnumerateDirectories())
                {
                    // If the package uses a non-default main file,
                    // add a redirect in index.js so that require()
                    // can find it without package.json.
                    if (module.Name != ".bin" && !File.Exists(Path.Combine(module.FullName, "index.js")))
                    {
                        dynamic package = Json.Decode(File.ReadAllText(Path.Combine(module.FullName, "package.json")));
                        string main = package.main;

                        if (!string.IsNullOrEmpty(main))
                        {
                            if (!main.StartsWith("."))
                                main = "./" + main;

                            File.WriteAllText(
                                Path.Combine(module.FullName, "index.js"),
                                "module.exports = require(" + Json.Encode(main) + ");"
                            );
                        }
                    }

                    string targetDir = Path.Combine(baseDir.FullName, "node_modules", module.Name);
                    if (!Directory.Exists(targetDir))
                        module.MoveTo(targetDir);
                    else if (module.Name != ".bin")
                        Log.LogMessage(MessageImportance.High, "Not collapsing conflicting module " + module.FullName);
                }

                if (!nodeModules.EnumerateFileSystemInfos().Any())
                    nodeModules.Delete();
            }
        }

        /// <summary>Invokes a command-line process asynchronously, capturing its output to a string.</summary>
        /// <returns>Null if the process exited successfully; the process' full output if it failed.</returns>
        static async Task<string> ExecWithOutputAsync(string filename, string args, string workingDirectory = null)
        {
            var error = new StringWriter();
            int result = await ExecAsync(filename, args, workingDirectory, null, error);

            return result == 0 ? null : error.ToString().Trim();
        }

        /// <summary>Invokes a command-line process asynchronously.</summary>
        static Task<int> ExecAsync(string filename, string args, string workingDirectory = null, TextWriter stdout = null, TextWriter stderr = null)
        {
            stdout = stdout ?? TextWriter.Null;
            stderr = stderr ?? TextWriter.Null;

            var p = new Process
            {
                StartInfo = new ProcessStartInfo(filename, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = workingDirectory == null ? null : Path.GetFullPath(workingDirectory),
                },
                EnableRaisingEvents = true,
            };

            p.OutputDataReceived += (sender, e) =>
            {
                stdout.WriteLine(e.Data);
            };
            p.ErrorDataReceived += (sender, e) =>
            {
                stderr.WriteLine(e.Data);
            };

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            var processTaskCompletionSource = new TaskCompletionSource<int>();

            p.EnableRaisingEvents = true;
            p.Exited += (s, e) =>
            {
                p.WaitForExit();
                processTaskCompletionSource.TrySetResult(p.ExitCode);
            };

            return processTaskCompletionSource.Task;
        }

        void ExtractZipWithOverwrite(Stream sourceZip, string destinationDirectoryName)
        {
            using (var source = new ZipArchive(sourceZip, ZipArchiveMode.Read))
            {
                foreach (var entry in source.Entries)
                {
                    const string prefix = "node_modules/npm/node_modules/";

                    // Collapse nested node_modules folders to avoid MAX_PATH issues from Path.GetFullPath
                    var targetSubPath = entry.FullName;
                    if (targetSubPath.StartsWith(prefix) && targetSubPath.Length > prefix.Length)
                    {
                        // If there is another node_modules folder after the prefix, collapse them
                        var lastModule = entry.FullName.LastIndexOf("node_modules/");
                        if (lastModule > prefix.Length)
                            targetSubPath = targetSubPath.Remove(prefix.Length, lastModule + "node_modules/".Length - prefix.Length);
                        Log.LogMessage(MessageImportance.Low, entry.FullName + "\t=> " + targetSubPath);
                    }

                    var targetPath = Path.GetFullPath(Path.Combine(destinationDirectoryName, targetSubPath));

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    if (!targetPath.EndsWith(@"\"))
                        entry.ExtractToFile(targetPath, overwrite: true);
                }
            }
        }
    }
}
