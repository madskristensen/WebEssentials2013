using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    class CoffeeScriptMargin : MarginBase
    {
        public const string MarginName = "CoffeeScriptMargin";
        private CoffeeScriptCompiler _compiler;
        private int _projectFileCount, _projectFileStep;

        public CoffeeScriptMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        {
            _compiler = new CoffeeScriptCompiler(Dispatcher);
            _compiler.Completed += _compiler_Completed; //+= (s, e) => { OnCompilationDone(e.Result, e.State); };
        }

        public CoffeeScriptMargin()
        {
            // Used for project compilation
        }

        public void CompileProject(EnvDTE.Project project)
        {
            if (string.IsNullOrEmpty(project.FullName))
                return;

            if (!CompileEnabled)
                return;

            Logger.Log("Compiling CoffeeScript...");
            _projectFileCount = 0;

            try
            {
                string dir = ProjectHelpers.GetRootFolder(project);
                if (string.IsNullOrEmpty(dir))
                    return;

                var files = Directory.GetFiles(dir, "*.coffee", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    string jsFile = GetCompiledFileName(file, ".js", CompileToLocation);

                    if (EditorExtensionsPackage.DTE.Solution.FindProjectItem(file) != null &&
                        File.Exists(jsFile))
                    {
                        _projectFileCount++;

                        using (CoffeeScriptCompiler compiler = new CoffeeScriptCompiler(Dispatcher.CurrentDispatcher))
                        {
                            compiler.Completed += compiler_Completed;
                            compiler.Compile(File.ReadAllText(file), file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        void compiler_Completed(object sender, CompilerEventArgs e)
        {
            _projectFileStep++;
            string file = GetCompiledFileName(e.State, ".js", CompileToLocation);

            ProjectHelpers.CheckOutFileFromSourceControl(file);

            using (StreamWriter writer = new StreamWriter(file, false, new UTF8Encoding(true)))
            {
                writer.Write(e.Result);
            }

            MinifyFile(e.State, e.Result);

            if (_projectFileStep == _projectFileCount)
                Logger.Log("CoffeeScript compiled");
            ((IDisposable)sender).Dispose();
        }

        protected override void StartCompiler(string source)
        {
            if (!CompileEnabled)
                return;

            string fileName = GetCompiledFileName(Document.FilePath, ".js", CompileToLocation);//Document.FilePath.Replace(".coffee", ".js");

            if (IsFirstRun && File.Exists(fileName))
            {
                OnCompilationDone(File.ReadAllText(fileName), Document.FilePath);
                return;
            }

            Logger.Log("CoffeeScript: Compiling " + Path.GetFileName(Document.FilePath));
            _compiler.Compile(source, Document.FilePath);
        }

        private void _compiler_Completed(object sender, CompilerEventArgs e)
        {
            if (e.Result.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                CompilerError error = ParseError(e.Result);
                CreateTask(error);
            }

            OnCompilationDone(e.Result, e.State);
        }

        private CompilerError ParseError(string error)
        {
            string message = error.Replace("ERROR:", string.Empty).Replace("Error:", string.Empty);
            int line = 0, column = 0;

            Match match = Regex.Match(message, @"^(\d{1,})[:](\d{1,})");
            if (match.Success)
            {
                if (!int.TryParse(match.Groups[1].Value, out line))
                {
                    line = 0;
                }
                if (!int.TryParse(match.Groups[2].Value, out column))
                {
                    column = 0;
                }
            }
            CompilerError result = new CompilerError()
            {
                Message = "CoffeeScript: " + message,
                FileName = Document.FilePath,
                Line = line,
                Column = column
            };

            return result;
        }

        public override void MinifyFile(string fileName, string source)
        {
            if (!CompileEnabled)
                return;

            if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptMinify))
            {
                string content = MinifyFileMenu.MinifyString(".js", source);
                string minFile = GetCompiledFileName(fileName, ".min.js", CompileToLocation);//fileName.Replace(".coffee", ".min.js");
                bool fileExist = File.Exists(minFile);

                ProjectHelpers.CheckOutFileFromSourceControl(minFile);
                using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(true)))
                {
                    writer.Write(content);
                }

                if (!fileExist)
                    AddFileToProject(fileName, minFile);
            }
        }

        public override bool CompileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.CoffeeScriptEnableCompiler); }
        }

        public override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.CoffeeScriptCompileToLocation); }
        }

        public override bool IsSaveFileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.GenerateJsFileFromCoffeeScript); }
        }

        protected override bool CanWriteToDisk(string source)
        {
            return !string.IsNullOrWhiteSpace(source);
        }
    }
}

//static class Iced
//{
//    [Export]
//    [FileExtension(".iced")]
//    [ContentType("CoffeeScript")]
//    internal static FileExtensionToContentTypeDefinition IcedFileExtensionDefinition;
//}