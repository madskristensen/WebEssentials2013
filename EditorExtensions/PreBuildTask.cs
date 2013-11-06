using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            var webclient = new WebClient();

            if (!File.Exists(@"resources\nodejs\node.exe"))
            {
                Log.LogMessage(MessageImportance.High, "Downloading nodejs ...");
                webclient.DownloadFile("http://nodejs.org/dist/v0.10.21/node.exe", @"resources\nodejs\node.exe");
            }

            if (!File.Exists(@"resources\nodejs\node_modules\npm\bin\npm.cmd"))
            {
                Log.LogMessage(MessageImportance.High, "Downloading npm ...");
                webclient.DownloadFile("http://nodejs.org/dist/npm/npm-1.3.13.zip", @"resources\nodejs\npm.zip");
                extractZipWithOverwrite(@"resources\nodejs\npm.zip", @"resources\nodejs");
                File.Delete(@"resources\nodejs\npm.zip");
            }

            if (!File.Exists(@"resources\nodejs\node_modules\.bin\lessc.cmd"))
            {
                Log.LogMessage(MessageImportance.High, "npm install less ...");
                var output = new StringWriter();
                int result = exec("cmd.exe", @"/c npm.cmd install less", @"resources\nodejs", output, output);
                if (result != 0)
                {
                    Log.LogError("npm error " + result + ": " + output.ToString().Trim());
                }
                flattenNodeModules(@"resources\nodejs\node_modules\less\node_modules");
            }

            return true;
        }

        /// <summary>
        /// Due to the way node_modues work, the directory depth can get very deep and go beyond MAX_PATH (260 chars). 
        /// Therefore grab all node_modues directories and move them up to baseNodeModuleDir. Node's require() will then 
        /// traverse up and find them at the higher level. Should be fine as long as there are no versioning conflicts.
        /// </summary>
        static void flattenNodeModules(string baseNodeModuleDir)
        {
            var baseDir = new DirectoryInfo(baseNodeModuleDir);

            var nodeModulesDirs = from dir in baseDir.EnumerateDirectories("*", SearchOption.AllDirectories)
                                  where dir.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                                  orderby dir.FullName.Split(Path.DirectorySeparatorChar).Length descending //get deepest first
                                  select dir;

            foreach (var nodeModules in nodeModulesDirs)
            {
                foreach (var module in nodeModules.GetDirectories())
                {
                    string targetDir = Path.Combine(baseDir.FullName, module.Name);
                    if (!Directory.Exists(targetDir))
                        module.MoveTo(targetDir);
                }

                if (nodeModules.GetFileSystemInfos().Length == 0)
                    nodeModules.Delete();
            }

        }

        /// <summary>Invokes a command-line process.</summary>
        static int exec(string filename, string args, string workingDirectory = null, TextWriter stdout = null, TextWriter stderr = null)
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
            p.WaitForExit();
            return p.ExitCode;
        }

        static void extractZipWithOverwrite(string sourceZip, string destinationDirectoryName)
        {
            using (var source = ZipFile.Open(sourceZip, ZipArchiveMode.Read))
            {
                foreach (var entry in source.Entries)
                {
                    var targetPath = Path.GetFullPath(Path.Combine(destinationDirectoryName, entry.FullName));

                    var isDirectory = (Path.GetFileName(targetPath).Length == 0);
                    if (isDirectory)
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        entry.ExtractToFile(targetPath, overwrite: true);
                    }
                }
            }
        }


    }
}
