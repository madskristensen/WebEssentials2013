using Microsoft.Ajax.Utilities;
using Microsoft.VisualStudio.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    internal class TypeScriptMargin : MarginBase
    {
        public const string MarginName = "TypeScriptMargin";
        private string _executablePath;

        public TypeScriptMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        {
            _executablePath = GetExecutablePath();
        }

        public TypeScriptMargin()
            : base()
        {
            _executablePath = GetExecutablePath();
        }

        public void CompileProjectFiles(EnvDTE.Project project)
        {
            try
            {
                if (!File.Exists(_executablePath) || string.IsNullOrEmpty(project.FullName))
                    return;

                string fullPath = project.Properties.Item("FullPath").Value.ToString();

                if (project != null && !string.IsNullOrEmpty(fullPath))
                {
                    string dir = Path.GetDirectoryName(fullPath);
                    var files = Directory.GetFiles(dir, "*.ts", SearchOption.AllDirectories);

                    Parallel.ForEach(files, file =>
                    {
                        if (!file.EndsWith(".d.ts") && EditorExtensionsPackage.DTE.Solution.FindProjectItem(file) != null)
                        {
                            StartProcess(file, CompileProjectExited);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void CompileProjectExited(object sender, EventArgs e)
        {
            Process p = (Process)sender;
            string file = p.StartInfo.EnvironmentVariables["file"];

            p.Exited -= CompileProjectExited;
            p.Dispose();

            string js = file.Replace(".ts", ".js");

            if (File.Exists(js))
            {
                try
                {
                    string content = File.ReadAllText(js);
                    MinifyFile(file, content);
                    ResaveWithBom(js, content);
                    Logger.Log("TypeScript: Compiling " + Path.GetFileName(file));
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }

            if (WESettings.GetBoolean(WESettings.Keys.TypeScriptAddGeneratedFilesToProject))
            {
                AddFileToProject(file);
            }
        }

        private void ResaveWithBom(string fileName, string content)
        {
            if (WESettings.GetBoolean(WESettings.Keys.TypeScriptResaveWithUtf8BOM))
            {
                using (StreamWriter writer = new StreamWriter(fileName, false, new UTF8Encoding(true)))
                {
                    writer.Write(content);
                }
            }
        }

        public override void MinifyFile(string fileName, string source)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.TypeScriptMinify))
                return;

            try
            {
                string filePath = fileName.Replace(".ts", ".js");
                if (File.Exists(filePath))
                {
                    Minifier minifier = new Minifier();
                    CodeSettings settings = new CodeSettings() { EvalTreatment = EvalTreatment.MakeImmediateSafe, PreserveImportantComments = false };

                    string content = minifier.MinifyJavaScript(source, settings);
                    string minFile = fileName.Replace(".ts", ".min.js");
                    bool fileExist = File.Exists(minFile);

                    using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(true)))
                    {
                        writer.Write(content);
                    }

                    if (!fileExist && WESettings.GetBoolean(WESettings.Keys.TypeScriptAddGeneratedFilesToProject))
                        AddFileToProject(fileName, minFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddFileToProject(string file)
        {
            string[] files = GetChildren(file);

            foreach (string generated in files)
            {
                if (EditorExtensionsPackage.DTE.Solution.FindProjectItem(generated) != null)
                    continue;

                if (File.Exists(generated))
                {
                    AddFileToProject(file, generated);
                }
            }
        }

        private static string[] GetChildren(string file)
        {
            return new string[] { 
                file.Replace(".ts", ".js"),
                file.Replace(".ts", ".min.js"),
                file.Replace(".ts", ".js.map")
            };
        }

        protected override void StartCompiler(string source)
        {
            string fileName = Document.FilePath.Replace(".ts", ".js");

            if (_isFirstRun && File.Exists(fileName))
            {
                OnCompilationDone(File.ReadAllText(fileName), Document.FilePath);
            }
            else if (!fileName.EndsWith(".d.ts") && WESettings.GetBoolean(WESettings.Keys.GenerateJsFileFromTypeScript))
            {
                if (EditorExtensionsPackage.DTE.Solution.SolutionBuild.BuildState == EnvDTE.vsBuildState.vsBuildStateInProgress)
                    return;

                if (File.Exists(_executablePath))
                {
                    _isFirstRun = false;
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        StartProcess(Document.FilePath, CompilerExited);
                    });
                }
                else
                {
                    base.OnCompilationDone("ERROR: The TypeScript compiler couldn't be found. Download http://www.typescriptlang.org/#Download", Document.FilePath);
                }
            }
            else
            {
                base.OnCompilationDone("// JavaScript generation is disabled in Tools -> Options", Document.FilePath);
            }
        }

        private static string GetExecutablePath()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string path = Path.Combine(programFiles, @"Microsoft SDKs\TypeScript\tsc.exe");

            if (!File.Exists(path))
            {
                path = Path.Combine(programFiles, @"Microsoft SDKs\TypeScript\0.8.1.1\tsc.exe");
            }

            if (!File.Exists(path))
            {
                path = Path.Combine(programFiles, @"Microsoft SDKs\TypeScript\0.8.1.0\tsc.exe");
            }

            if (!File.Exists(path))
            {
                path = Path.Combine(programFiles, @"Microsoft SDKs\TypeScript\0.8.0.0\tsc.exe");
            }

            return path;
        }

        private void StartProcess(string file, EventHandler eventHandler)
        {
            CheckOutChildren(file);

            Logger.Log("Compiling TypeScript...");

            ProcessStartInfo start = new ProcessStartInfo();
            start.WindowStyle = ProcessWindowStyle.Hidden;
            start.CreateNoWindow = true;
            start.Arguments = "\"" + file + "\"" + GenerateArguments();
            start.FileName = _executablePath;
            start.UseShellExecute = false;
            start.EnvironmentVariables.Add("file", file);
            start.RedirectStandardError = true;

            Process p = new Process();
            p.StartInfo = start;
            p.EnableRaisingEvents = true;
            p.Exited += eventHandler;

            p.Start();
        }

        private static void CheckOutChildren(string file)
        {
            var files = GetChildren(file).Where(f => File.Exists(f));

            foreach (string child in files)
            {
                ProjectHelpers.CheckOutFileFromSourceControl(child);
            }
        }

        private static string GenerateArguments()
        {
            string args = string.Empty;

            if (WESettings.GetBoolean(WESettings.Keys.TypeScriptUseAmdModule))
                args += " --module amd";

            if (WESettings.GetBoolean(WESettings.Keys.TypeScriptCompileES3))
                args += " --target ES3";
            else
                args += " --target ES5";

            if (WESettings.GetBoolean(WESettings.Keys.TypeScriptProduceSourceMap))
                args += " -sourcemap";

            if (WESettings.GetBoolean(WESettings.Keys.TypeScriptKeepComments))
                args += " -c";

            return args;

        }

        private void CompilerExited(object sender, EventArgs e)
        {
            Process p = (Process)sender;

            if (p.ExitCode == 0)
            {
                string fileName = Document.FilePath.Replace(".ts", ".js");

                if (File.Exists(fileName))
                {
                    string content = File.ReadAllText(fileName);

                    ResaveWithBom(fileName, content);

                    if (WESettings.GetBoolean(WESettings.Keys.TypeScriptAddGeneratedFilesToProject))
                    {
                        AddFileToProject(Document.FilePath);
                    }

                    Logger.Log("TypeScript: Compiling " + Path.GetFileName(fileName));
                    base.OnCompilationDone(content, Document.FilePath);
                }
            }
            else
            {
                base.OnCompilationDone("ERROR: " + p.StandardError.ReadToEnd(), Document.FilePath);
            }

            p.Exited -= CompilerExited;
            p.Dispose();
        }

        public override bool IsSaveFileEnabled
        {
            get { return false; }
        }

        public override bool UseCompiledFolder
        {
            get { return false; }
        }

        protected override bool CanWriteToDisk(string source)
        {
            return false;
        }
    }
}
