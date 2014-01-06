using System;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    public class SassMargin : MarginBase
    {
        public SassMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, contentType, showMargin, document)
        { }

        protected override async void StartCompiler(string source)
        {
            string sassFilePath = Document.FilePath;
            string cssFilename = GetCompiledFileName(sassFilePath, ".css", CompileToLocation);

            if (!IsSaveFileEnabled)
                cssFilename = Path.GetTempFileName();

            if (IsFirstRun && File.Exists(cssFilename))
            {
                OnCompilationDone(File.ReadAllText(cssFilename), sassFilePath);
                return;
            }

            Logger.Log("SASS: Compiling " + Path.GetFileName(sassFilePath));

            var result = await new SassCompiler().Compile(sassFilePath, cssFilename);

            if (result.IsSuccess)
            {
                OnCompilationDone(result.Result, result.FileName);
            }
            else
            {
                result.Errors.First().Message = "SASS: " + result.Errors.First().Message;

                CreateTask(result.Errors.First());

                base.OnCompilationDone("ERROR:" + result.Errors.First().Message, sassFilePath);
            }
        }

        protected override void MinifyFile(string fileName, string source)
        {
            if (!IsSaveFileEnabled)
                return;

            if (WESettings.GetBoolean(WESettings.Keys.SassMinify) && !Path.GetFileName(fileName).StartsWith("_", StringComparison.Ordinal))
            {
                FileHelpers.MinifyFile(fileName, source, ".css");
            }
        }
        
        public override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.SassCompileToLocation); }
        }

        public override bool IsSaveFileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromSass) && !Path.GetFileName(Document.FilePath).StartsWith("_", StringComparison.Ordinal); }
        }
    }
}