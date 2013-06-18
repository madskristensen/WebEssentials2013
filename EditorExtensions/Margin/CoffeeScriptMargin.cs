using Microsoft.VisualStudio.Text;
using System;
using System.IO;
using System.Text;
using System.Windows.Threading;

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

            Logger.Log("Compiling CoffeeScript...");
            _projectFileCount = 0;

            try
            {
                string fullPath = project.Properties.Item("FullPath").Value.ToString();

                if (project != null && !string.IsNullOrEmpty(fullPath))
                {
                    string dir = Path.GetDirectoryName(fullPath);
                    var files = Directory.GetFiles(dir, "*.coffee", SearchOption.AllDirectories);

                    foreach (string file in files)
                    {
                        string jsFile = GetCompiledFileName(file, ".js", UseCompiledFolder);

                        if (EditorExtensionsPackage.DTE.Solution.FindProjectItem(file) != null &&
                            File.Exists(jsFile))
                        {
                            _projectFileCount++;

                            CoffeeScriptCompiler compiler = new CoffeeScriptCompiler(Dispatcher.CurrentDispatcher);
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
            string file = GetCompiledFileName(e.State, ".js", UseCompiledFolder);

            ProjectHelpers.CheckOutFileFromSourceControl(file);

            using (StreamWriter writer = new StreamWriter(file, false, new UTF8Encoding(true)))
            {
                writer.Write(e.Result);
            }

            MinifyFile(e.State, e.Result);

            if (_projectFileStep == _projectFileCount)
                Logger.Log("CoffeeScript compiled");
        }

        protected override void StartCompiler(string source)
        {
            string fileName = GetCompiledFileName(Document.FilePath, ".js", UseCompiledFolder);//Document.FilePath.Replace(".coffee", ".js");

            if (_isFirstRun && File.Exists(fileName))
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
            int index = message.IndexOf(':');
            int line = 0;

            if (index > -1)
            {
                int start = message.LastIndexOf(' ', index);
                if (start > -1)
                {
                    int length = index - start - 1;
                    string part = message.Substring(start + 1, length);
                    int.TryParse(part, out line);
                }
            }

            CompilerError result = new CompilerError()
            {
                Message = "CoffeeScript: " + message,
                FileName = Document.FilePath,
                Line = line,
            };

            return result;
        }

        public override void MinifyFile(string fileName, string source)
        {
            if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptMinify))
            {
                string content = MinifyFileMenu.MinifyString(".js", source);
                string minFile = GetCompiledFileName(fileName, ".min.js", UseCompiledFolder);//fileName.Replace(".coffee", ".min.js");
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

        public override bool UseCompiledFolder
        {
            get { return WESettings.GetBoolean(WESettings.Keys.CoffeeScriptCompileToFolder); }
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