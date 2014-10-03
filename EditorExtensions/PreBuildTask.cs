using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
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
        Type _path;
        Type _directory;
        Type _directoryInfo;
        Type _file;

        public PreBuildTask()
        {
            try
            {
                Assembly a = null;
                a = Assembly.LoadFrom(Path.GetDirectoryName(Environment.CurrentDirectory) + @"\packages\Pri.LongPath.1.2.2.0\lib\net45\Pri.LongPath.dll");
                _path = a.GetType("Pri.LongPath.Path");
                _directory = a.GetType("Pri.LongPath.Directory");
                _directoryInfo = a.GetType("Pri.LongPath.DirectoryInfo");
                _file = a.GetType("Pri.LongPath.File");
            }
            catch (Exception)
            { }
        }

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

            if (!FlattenModulesAsync().Result)
                return false;

            return true;
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

            Log.LogMessage(MessageImportance.High, "Downloading npm ...");

            var npmZip = await WebClientDoAsync(wc => wc.OpenReadTaskAsync("http://nodejs.org/dist/npm/npm-1.3.23.zip"));

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

        async Task<bool> FlattenModulesAsync()
        {
            var output = await ExecWithOutputAsync(@"cmd", @"/c ..\npm.cmd dedup ", @"resources\nodejs\tools");

            if (output != null)
            {
                Log.LogError("npm dedup error: " + output);

                return false;
            }

            FlattenNodeModules(@"resources\nodejs\tools");

            return true;
        }

        /// <summary>
        /// Due to the way node_modues work, the directory depth can get very deep and go beyond MAX_PATH (260 chars). 
        /// Therefore grab all node_modues directories and move them up to baseNodeModuleDir. Node's require() will then 
        /// traverse up and find them at the higher level. Should be fine as long as there are no versioning conflicts.
        /// </summary>
        void FlattenNodeModules(string baseNodeModuleDir)
        {
            var baseDir = new DirectoryInfo(baseNodeModuleDir);
            object instance = Activator.CreateInstance(_directoryInfo, new Object[] { baseNodeModuleDir });
            MethodInfo enumerateDirectories = _directoryInfo.GetMethod("EnumerateDirectories", new Type[] { typeof(string), typeof(SearchOption) });

            var nodeModulesDirs = from dir in (IEnumerable<dynamic>)enumerateDirectories.Invoke(instance, new object[] { "*", SearchOption.AllDirectories })
                                  where dir.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                                  //orderby dir.FullName.ToString().Count(c => c == Path.DirectorySeparatorChar) descending // Get deepest first
                                  select dir;

            // Since IEnumerable<dynamic> can't use orderby (throws CS1977), we will use custom sort.
            var nodeModulesDirsList = nodeModulesDirs.ToList();
            nodeModulesDirsList.Sort((dir1, dir2) => dir2.FullName.Split(Path.DirectorySeparatorChar).Length.CompareTo(dir1.FullName.Split(Path.DirectorySeparatorChar).Length));

            foreach (var nodeModules in nodeModulesDirsList)
            {
                foreach (var module in nodeModules.EnumerateDirectories())
                {
                    // If the package uses a non-default main file,
                    // add a redirect in index.js so that require()
                    // can find it without package.json.
                    if (module.Name != ".bin" && !File.Exists(Path.Combine(module.FullName, "index.js")))
                    {
                        enumerateDirectories = _file.GetMethod("ReadAllText", new Type[] { typeof(string) });
                        string path = (string)enumerateDirectories.Invoke(null, new object[] { module.FullName + "\\package.json" });
                        dynamic package = Json.Decode(path);
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
                    {
                        enumerateDirectories = _directoryInfo.GetMethod("MoveTo", new Type[] { typeof(string) });
                        //module.MoveTo(targetDir);
                        enumerateDirectories.Invoke(module, new object[] { targetDir });
                    }
                    else if (module.Name != ".bin")
                        Log.LogMessage(MessageImportance.High, "Not collapsing conflicting module " + module.FullName);
                }

                enumerateDirectories = _directoryInfo.GetMethod("EnumerateFileSystemInfos", Type.EmptyTypes);

                if (!(enumerateDirectories.Invoke(nodeModules, new object[] { }) as IEnumerable<dynamic>).Any())
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
