using System;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    public class LessMargin : MarginBase
    {
        public LessMargin(string contentType, string source, ITextDocument document)
            : base(source, contentType, WESettings.Instance.Less, document)
        { }

        protected override async void StartCompiler(string source)
        {
            string lessFilePath = Document.FilePath;
            string cssFilename = GetCompiledFileName(lessFilePath, ".css", CompileToLocation);

            if (!IsSaveFileEnabled) // Path.GetTempFileName() creates the file. We don't want that
                cssFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");

            if (IsFirstRun && File.Exists(cssFilename))
            {
                OnCompilationDone(File.ReadAllText(cssFilename), lessFilePath);
                return;
            }

            Logger.Log("LESS: Compiling " + Path.GetFileName(lessFilePath));

            var result = await new LessCompiler().CompileAsync(lessFilePath, cssFilename);

            if (result == null)
                return;

            if (result.IsSuccess)
            {
                OnCompilationDone(result.Result, result.SourceFileName);
            }
            else
            {
                result.Errors.First().Message = "LESS: " + result.Errors.First().Message;

                CreateTask(result.Errors.First());

                base.OnCompilationDone("ERROR:" + result.Errors.First().Message, lessFilePath);
            }
        }

        protected override void MinifyFile(string fileName, string source)
        {
            if (WESettings.GetBoolean(WESettings.Keys.LessMinify) && !Path.GetFileName(fileName).StartsWith("_", StringComparison.Ordinal))
            {
                FileHelpers.MinifyFile(fileName, source, ".css");
            }
        }

        public override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.LessCompileToLocation); }
        }

        public override bool IsSaveFileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromLess) && !Path.GetFileName(Document.FilePath).StartsWith("_", StringComparison.Ordinal); }
        }
    }
}