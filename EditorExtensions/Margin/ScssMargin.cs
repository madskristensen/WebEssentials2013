//using EnvDTE;
//using Microsoft.CSS.Core;
//using Microsoft.VisualStudio.Text;
//using SassAndCoffee.Ruby.Sass;
//using System;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;

//namespace MadsKristensen.EditorExtensions
//{
//    /// <summary>
//    /// A class detailing the margin's visual definition including both size and content.
//    /// </summary>
//    class ScssMargin : MarginBase
//    {
//        public const string MarginName = "ScssMargin";
//        private SassCompiler _compiler;

//        public ScssMargin()
//            : base()
//        {
//            _compiler = new SassCompiler();
//        }

//        public ScssMargin(string contentType, string source, bool showMargin, ITextDocument document)
//            : base(source, MarginName, contentType, showMargin, document)
//        {
//            _compiler = new SassCompiler();
//        }

//        public void CompileProject()
//        {
//            Project project = ProjectHelpers.GetActiveProject();

//            if (project != null && !string.IsNullOrEmpty(project.FullName))
//            {
//                Task.Run(() =>
//                {
//                    string dir = Path.GetDirectoryName(project.FullName);
//                    var files = Directory.GetFiles(dir, "*.scss", SearchOption.AllDirectories).Where(f => CanCompile(f));

//                    Parallel.ForEach(files, file =>
//                    {
//                        EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Compiling " + Path.GetFileName(file);
//                        string result = CompileFile(file);
//                        base.WriteCompiledFile(result, file);
//                        this.MinifyFile(file, result);
//                    });

//                    EditorExtensionsPackage.DTE.StatusBar.Clear();
//                });
//            }
//        }

//        private static bool CanCompile(string fileName)
//        {
//            if (EditorExtensionsPackage.DTE.Solution.FindProjectItem(fileName) == null)
//                return false;

//            if (Path.GetFileName(fileName).StartsWith("_"))
//                return false;

//            string minFile = MarginBase.GetCompiledFileName(fileName, ".min.css", WESettings.GetBoolean(WESettings.Keys.LessCompileToFolder));
//            if (File.Exists(minFile) && WESettings.GetBoolean(WESettings.Keys.LessMinify))
//                return true;

//            string cssFile = MarginBase.GetCompiledFileName(fileName, ".css", WESettings.GetBoolean(WESettings.Keys.LessCompileToFolder));
//            if (!File.Exists(cssFile))
//                return false;


//            return true;
//        }


//        protected override void StartCompiler(string source)
//        {
//            string fileName = GetCompiledFileName(Document.FilePath, ".css", UseCompiledFolder);

//            if (_isFirstRun && File.Exists(fileName))
//            {
//                OnCompilationDone(File.ReadAllText(fileName), Document.FilePath);
//                return;
//            }
//            else if (!Path.GetFileName(Document.FilePath).StartsWith("_"))
//            {
//                Task.Run(() =>
//                {
//                    Compile(Document.FilePath);
//                });
//            }
//        }

//        private void Compile(string fileName)
//        {
//            EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Compiling " + Path.GetFileName(fileName);

//            string result = CompileFile(fileName);
//            OnCompilationDone(result, Document.FilePath);

//            EditorExtensionsPackage.DTE.StatusBar.Clear();
//        }

//        private string CompileFile(string fileName)
//        {
//            try
//            {
//                string result = _compiler.Compile(fileName, false, null);

//                CssFormatter formatter = new CssFormatter();
//                result = formatter.Format(result);

//                return result;
//            }
//            catch (Exception ex)
//            {
//                return "ERROR: " + ex.Message;
//            }
//        }

//        public override void MinifyFile(string fileName, string source)
//        {
//            if (WESettings.GetBoolean(WESettings.Keys.ScssMinify))
//            {
//                string content = MinifyFileMenu.MinifyString(".css", source);
//                string minFile = GetCompiledFileName(fileName, ".min.css", UseCompiledFolder);
//                bool fileExist = File.Exists(minFile);

//                ProjectHelpers.CheckOutFileFromSourceControl(minFile);
//                File.WriteAllText(minFile, content);

//                if (!fileExist)
//                    AddFileToProject(Document.FilePath, minFile);
//            }
//        }

//        public override bool CanTakeFocus
//        {
//            get { return true; }
//        }

//        public override bool IsSaveFileEnabled
//        {
//            get { return WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromScss) && !Path.GetFileName(Document.FilePath).StartsWith("_"); }
//        }

//        public override bool UseCompiledFolder
//        {
//            get { return WESettings.GetBoolean(WESettings.Keys.ScssCompileToFolder); }
//        }

//        protected override bool CanWriteToDisk(string source)
//        {
//            return !string.IsNullOrWhiteSpace(source);
//        }
//    }
//}