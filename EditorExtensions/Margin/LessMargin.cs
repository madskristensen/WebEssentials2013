using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    public class LessMargin : MarginBase
    {
        public LessMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, contentType, showMargin, document)
        { }

        protected override async void StartCompiler(string source)
        {
            string lessFilePath = Document.FilePath;
            string cssFilename = GetCompiledFileName(lessFilePath, ".css", CompileToLocation);

            if (!IsSaveFileEnabled)
                cssFilename = Path.GetTempFileName();

            if (IsFirstRun && File.Exists(cssFilename))
            {
                OnCompilationDone(File.ReadAllText(cssFilename), lessFilePath);
                return;
            }

            Logger.Log("LESS: Compiling " + Path.GetFileName(lessFilePath));

            string preProcessedLessFilePath = GetPreProcessedFileName(Document.FilePath);
            var result = await new LessCompiler().Compile(preProcessedLessFilePath, cssFilename);
            if (result == null)
            {
                File.Delete(preProcessedLessFilePath);
                return;
            }

            if (result.IsSuccess)
            {
                OnCompilationDone(result.Result, result.FileName);
            }
            else
            {
                result.Errors.First().Message = "LESS: " + result.Errors.First().Message;

                CreateTask(result.Errors.First());

                base.OnCompilationDone("ERROR:" + result.Errors.First().Message, lessFilePath);
            }

            File.Delete(preProcessedLessFilePath);
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

        private static readonly Regex _referenceCommentPattern = new Regex(@"///\s*<reference\s+path=(['""])(?<path>[^'""]+)\1(\s*/>)?");
        private string GetPreProcessedFileName(string sourceFileName)
        {
            string preProcessedFileName = GetCompiledFileName(sourceFileName, ".obj.less", CompileToLocation);

            var lines = File.ReadAllLines(sourceFileName).Select(line =>
            {
                var matches = _referenceCommentPattern.Matches(line).Cast<Match>().ToArray();
                if (matches.Length == 0)
                    return line;

                var imports = string.Empty;
                foreach (var match in matches)
                {
                    var path = match.Groups["path"].Value;
                    if (path.StartsWith("~/"))
                        path = Path.Combine(ProjectHelpers.GetProjectFolder(sourceFileName), path.Substring(2));

                    imports += "@import (reference) \"" + path + "\";";
                }

                return imports;
            });

            File.WriteAllLines(preProcessedFileName, lines);
            return preProcessedFileName;
        }
    }
}