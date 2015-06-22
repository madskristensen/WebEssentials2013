using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Pri.LongPath;
using IO = System.IO;

namespace WebEssentials.BuildTasks
{
    public class NodeInstaller : Microsoft.Build.Utilities.Task
    {
        private List<string> toRemove = new List<string>()
        {
            "*.md",
            "*.markdown",
            "*.html",
            "*.txt",
            "LICENSE",
            "README",
            "CHANGELOG",
            "CNAME",
            "*.old",
            "*.patch",
            "*.ico",
            "Makefile.*",
            "Rakefile",
            "*.yml",
            "test.*",
            "generate-*",
            "media",
            "images",
            "man",
            "benchmark",
            "docs",
            "scripts",
            "test",
            "tst",
            "tests",
            "testing",
            "examples",
            "*.tscache",
            "example",
        };

        static DateTime GetSourceVersion([CallerFilePath] string path = null) { return File.GetLastWriteTimeUtc(path); }


        // Stores the timestamp of the last successful build.  This file will be deleted
        // at the beginning of each non-cached build, so there is no risk of caching the
        // results of a failed build.
        const string VersionStampFileName = @"resources\nodejs\tools\node_modules\successful-version-timestamp.txt";
        public override bool Execute()
        {
            DateTime existingVersion;
            if (File.Exists(VersionStampFileName)
             && DateTime.TryParse(
                 File.ReadAllText(VersionStampFileName),
                 CultureInfo.InvariantCulture,
                 DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal,
                 out existingVersion)
             && existingVersion > DateTime.UtcNow - TimeSpan.FromDays(14)
             && existingVersion > GetSourceVersion())
            {
                Log.LogMessage(MessageImportance.High, "Reusing existing installed Node modules from " + existingVersion);
                return true;
            }
            if (Directory.Exists(@"resources\nodejs\tools\node_modules"))
                ClearPath(@"resources\nodejs\tools\node_modules");

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
                InstallModuleAsync("handlebars", "handlebars"),
                InstallModuleAsync("jshint", "jshint"),
                InstallModuleAsync("tslint", "tslint"),
                InstallModuleAsync("node-sass", "node-sass"),
                InstallModuleAsync("coffee", "coffee-script"),
                InstallModuleAsync("autoprefixer", "autoprefixer"),
                InstallModuleAsync("iced", "iced-coffee-script"),
                InstallModuleAsync("LiveScript", "LiveScript"),
                InstallModuleAsync("coffeelint", "coffeelint"),
                InstallModuleAsync("sjs", "sweet.js"),
                InstallModuleAsync(null, "xregexp"),
                InstallModuleAsync("rtlcss", "rtlcss"),
                InstallModuleAsync("cson", "cson")
            ).Result.Where(r => r != ModuleInstallResult.AlreadyPresent);

            if (moduleResults.Contains(ModuleInstallResult.Error))
                return false;

            if (!moduleResults.Any())
                return true;

            Log.LogMessage(MessageImportance.High, "Installed " + moduleResults.Count() + " modules.  Flattening...");

            if (!DedupeAsync().Result)
                return false;

            // Delete test directories before flattening (since some tests have node_modules folders)
            CleanPath(@"resources\nodejs\tools\node_modules");
            FlattenNodeModules(@"resources\nodejs\tools");

            File.WriteAllText(VersionStampFileName, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

            return true;
        }

        private void ClearPath(string path)
        {
            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                Log.LogMessage(MessageImportance.Low, "Removing " + dir + "...");
                Directory.Delete(dir, true);
            }

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                Log.LogMessage(MessageImportance.Low, "Removing " + file + "...");
                File.Delete(file);
            }
        }

        private void CleanPath(string path)
        {
            Log.LogMessage(MessageImportance.High, "Cleaning extra files from " + path + "...");
            int count = 0;
            foreach (string pattern in toRemove)
            {
                string[] dirs = Directory.GetDirectories(path, pattern, IO.SearchOption.AllDirectories);
                foreach (string dir in dirs)
                    Directory.Delete(dir, true);
                count += dirs.Length;

                string[] files = Directory.GetFiles(path, pattern, IO.SearchOption.AllDirectories);
                foreach (string file in files)
                    File.Delete(file);
                count += files.Length;
            }
            Log.LogMessage(MessageImportance.High, "Deleted " + count + " items");
        }

        Task DownloadNodeAsync()
        {
            var file = new FileInfo(@"resources\nodejs\node.exe");

            if (file.Exists && file.Length > 0)
                return Task.FromResult<object>(null);

            Log.LogMessage(MessageImportance.High, "Downloading nodejs ...");
            return WebClientDoAsync(wc => wc.DownloadFileTaskAsync("http://nodejs.org/dist/latest/node.exe", @"resources\nodejs\node.exe"));
        }

        async Task DownloadNpmAsync()
        {
            var file = new FileInfo(@"resources\nodejs\node_modules\npm\bin\npm.cmd");

            if (file.Exists && file.Length > 0)
                return;

            await WebClientDoAsync(wc => wc.DownloadFileTaskAsync("https://raw.githubusercontent.com/joyent/node/master/deps/npm/package.json", @"resources\nodejs\package.json"));

            dynamic nodeInfo = JsonConvert.DeserializeObject(File.ReadAllText(@"resources\nodejs\package.json"));
            string npmVersion = nodeInfo.version;

            string npmUrl = string.Format(CultureInfo.CurrentCulture, "https://github.com/npm/npm/archive/v{0}.zip", npmVersion);

            File.Delete(@"resources\nodejs\package.json");

            Log.LogMessage(MessageImportance.High, "Downloading npm ...");

            var npmZip = await WebClientDoAsync(wc => wc.OpenReadTaskAsync(npmUrl));

            try
            {
                ExtractZipWithOverwrite(npmZip, @"resources\nodejs", npmVersion);
            }
            catch
            {
                // Make sure the next build doesn't see a half-installed npm
                var npmDirectory = @"resources\nodejs\node_modules\npm";

                if (Directory.Exists(npmDirectory))
                    Directory.Delete(npmDirectory, true);

                throw;
            }

            var npmDestination = @"resources\nodejs\npm.cmd";

            if (File.Exists(npmDestination))
                File.Delete(npmDestination);

            File.Move(string.Format(@"resources\nodejs\node_modules\npm\bin\npm.cmd", npmVersion), npmDestination);
        }

        async Task WebClientDoAsync(Func<WebClient, Task> transactor)
        {
            try
            {
                await transactor(new WebClient());
                return;
            }
            catch (WebException e)
            {
                Log.LogWarningFromException(e);
                if (!IsHttpStatusCode(e, HttpStatusCode.ProxyAuthenticationRequired))
                    throw;
            }

            await transactor(CreateWebClientWithProxyAuthSetup());
        }

        async Task<T> WebClientDoAsync<T>(Func<WebClient, Task<T>> transactor)
        {
            try
            {
                return await transactor(new WebClient());
            }
            catch (WebException e)
            {
                Log.LogWarningFromException(e);
                if (!IsHttpStatusCode(e, HttpStatusCode.ProxyAuthenticationRequired))
                    throw;
            }

            return await transactor(CreateWebClientWithProxyAuthSetup());
        }

        static bool IsHttpStatusCode(WebException e, HttpStatusCode status)
        {
            HttpWebResponse response;
            return e.Status == WebExceptionStatus.ProtocolError
                && (response = e.Response as HttpWebResponse) != null
                && response.StatusCode == status;
        }

        static WebClient CreateWebClientWithProxyAuthSetup(IWebProxy proxy = null, ICredentials credentials = null)
        {
            var wc = new WebClient { Proxy = proxy ?? WebRequest.GetSystemWebProxy() };
            wc.Proxy.Credentials = credentials ?? CredentialCache.DefaultCredentials;
            return wc;
        }

        enum ModuleInstallResult { AlreadyPresent, Installed, Error }

        async Task<ModuleInstallResult> InstallModuleAsync(string cmdName, string moduleName)
        {
            if (string.IsNullOrEmpty(cmdName))
            {
                if (File.Exists(@"resources\nodejs\tools\node_modules\" + moduleName + @"\package.json"))
                    return ModuleInstallResult.AlreadyPresent;
            }
            else
            {
                if (File.Exists(@"resources\nodejs\tools\node_modules\.bin\" + cmdName + ".cmd"))
                    return ModuleInstallResult.AlreadyPresent;
            }

            Log.LogMessage(MessageImportance.High, "npm install " + moduleName + " ...");

            var output = await ExecWithOutputAsync(@"cmd", @"/c ..\npm.cmd install " + moduleName, @"resources\nodejs\tools");

            if (output != null)
            {
                Log.LogError("npm install " + moduleName + " error: " + output);
                return ModuleInstallResult.Error;
            }

            return ModuleInstallResult.Installed;
        }

        async Task<bool> DedupeAsync()
        {
            var output = await ExecWithOutputAsync(@"cmd", @"/c ..\npm.cmd dedup ", @"resources\nodejs\tools");

            if (output != null)
                Log.LogError("npm dedup error: " + output);

            return output == null;
        }

        /// <summary>
        /// Due to the way node_modues work, the directory depth can get very deep and go beyond MAX_PATH (260 chars). 
        /// Therefore grab all node_modues directories and move them up to baseNodeModuleDir. Node's require() will then 
        /// traverse up and find them at the higher level. Should be fine as long as there are no versioning conflicts.
        /// </summary>
        void FlattenNodeModules(string baseNodeModuleDir)
        {
            var baseDir = new DirectoryInfo(baseNodeModuleDir);

            var modules = from dir in new DirectoryInfo(baseNodeModuleDir).GetDirectories("*", IO.SearchOption.AllDirectories)
                          where dir.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                          orderby dir.FullName.Count(c => c == Path.DirectorySeparatorChar) descending // Get deepest first
                          select dir;

            foreach (var nodeModule in modules)
            {
                foreach (var module in nodeModule.EnumerateDirectories())
                {
                    // If the package uses a non-default main file,
                    // add a redirect in index.js so that require()
                    // can find it without package.json.
                    if (module.Name != ".bin" && !File.Exists(Path.Combine(module.FullName, "index.js")))
                    {
                        dynamic package = JsonConvert.DeserializeObject(File.ReadAllText(module.FullName + "\\package.json"));
                        string main = package.main;

                        if (!string.IsNullOrEmpty(main))
                        {
                            if (!main.StartsWith("."))
                                main = "./" + main;
                            File.WriteAllText(
                                Path.Combine(module.FullName, "index.js"),
                                "module.exports = require(" + JsonConvert.ToString(main) + ");"
                            );
                        }
                    }

                    // If this is already a top-level module, don't move it.
                    if (module.Parent.Parent.FullName == baseDir.FullName)
                        continue;
                    else if (module.Name == ".bin")
                    {
                        // We don't care about any .bin folders in nested modules (we do need the top-level one)
                        module.Delete(recursive: true);
                        continue;
                    }

                    var intermediatePath = baseDir.FullName;
                    dynamic sourcePackage = JsonConvert.DeserializeObject(File.ReadAllText(module.FullName + @"\package.json"));
                    // Try to move the module to the node_modules folder in the
                    // base directory, then to that same folder in every parent
                    // module up to this module's immediate parent.
                    foreach (var part in module.Parent.Parent.FullName.Substring(intermediatePath.Length).Split(new[] { @"\node_modules\" }, StringSplitOptions.None))
                    {
                        if (!string.IsNullOrEmpty(part))
                            intermediatePath += @"\node_modules\" + part;
                        string targetDir = Path.Combine(intermediatePath, "node_modules", module.Name);
                        if (Directory.Exists(targetDir))
                        {
                            dynamic targetPackage = JsonConvert.DeserializeObject(File.ReadAllText(targetDir + @"\package.json"));
                            // If the existing package is a different version, keep
                            // going, and move it to a different folder. Otherwise,
                            // delete it and keep the other one, then stop looking.
                            if (targetPackage.version != sourcePackage.version)
                                continue;
                            Log.LogMessage(MessageImportance.High, "Deleting " + module.FullName + " in favor of " + targetDir);
                            module.Delete(recursive: true);
                            break;
                        }
                        module.MoveTo(targetDir);
                        break;
                    }
                    if (module.Exists)
                        Log.LogMessage(MessageImportance.High, "Not collapsing conflicting module " + module.FullName);
                }

                if (!nodeModule.GetFileSystemInfos().Any())
                    nodeModule.Delete();
            }
        }

        /// <summary>Invokes a command-line process asynchronously, capturing its output to a string.</summary>
        /// <returns>Null if the process exited successfully; the process' full output if it failed.</returns>
        static async Task<string> ExecWithOutputAsync(string filename, string args, string workingDirectory = null)
        {
            var error = new IO.StringWriter();
            int result = await ExecAsync(filename, args, workingDirectory, null, error);

            return result == 0 ? null : error.ToString().Trim();
        }

        /// <summary>Invokes a command-line process asynchronously.</summary>
        static Task<int> ExecAsync(string filename, string args, string workingDirectory = null, IO.TextWriter stdout = null, IO.TextWriter stderr = null)
        {
            stdout = stdout ?? IO.TextWriter.Null;
            stderr = stderr ?? IO.TextWriter.Null;

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

        void ExtractZipWithOverwrite(IO.Stream sourceZip, string destinationDirectoryName, string version)
        {
            using (var source = new ZipArchive(sourceZip, ZipArchiveMode.Read))
            {
                foreach (var entry in source.Entries)
                {
                    const string prefix = "node_modules/npm/node_modules/";

                    // Collapse nested node_modules folders to avoid MAX_PATH issues from Path.GetFullPath
                    var targetSubPath = entry.FullName.Replace(string.Format("npm-{0}/", version), "node_modules/npm/");
                    if (targetSubPath.StartsWith(prefix) && targetSubPath.Length > prefix.Length)
                    {
                        // If there is another node_modules folder after the prefix, collapse them
                        var lastModule = targetSubPath.LastIndexOf("node_modules/");
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
