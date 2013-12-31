using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    public class LessMargin : MarginBase
    {
        public const string MarginName = "LessMargin";
        private static readonly Regex _sourceMapinCSS = new Regex(@"\/\*#([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*\/", RegexOptions.Multiline);

        public LessMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        { }

        protected override async void StartCompiler(string source)
        {
            if (!CompileEnabled)
                return;

            string lessFilePath = Document.FilePath;

            string cssFilename = GetCompiledFileName(lessFilePath, ".css", CompileEnabled ? CompileToLocation : null);// Document.FilePath.Replace(".less", ".css");

            if (IsFirstRun && File.Exists(cssFilename))
            {
                OnCompilationDone(File.ReadAllText(cssFilename), lessFilePath);
                return;
            }

            Logger.Log("LESS: Compiling " + Path.GetFileName(lessFilePath));

            var result = await new LessCompiler().Compile(lessFilePath, cssFilename);

            if (result.IsSuccess)
            {
                OnCompilationDone(result.Result, result.FileName);
            }
            else
            {
                result.Error.Message = "LESS: " + result.Error.Message;

                CreateTask(result.Error);

                base.OnCompilationDone("ERROR:" + result.Error.Message, lessFilePath);
            }
        }

        public override void MinifyFile(string fileName, string source)
        {
            if (!CompileEnabled)
                return;

            if (WESettings.GetBoolean(WESettings.Keys.LessMinify) && !Path.GetFileName(fileName).StartsWith("_", StringComparison.Ordinal))
            {
                FileHelpers.MinifyFile(fileName, source, ".css");
            }
        }

        public override bool CompileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.LessEnableCompiler); }
        }

        public override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.LessCompileToLocation); }
        }

        public override bool IsSaveFileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromLess) && !Path.GetFileName(Document.FilePath).StartsWith("_", StringComparison.Ordinal); }
        }

        protected override bool CanWriteToDisk(string source)
        {
            //var parser = new Microsoft.CSS.Core.CssParser();
            //StyleSheet stylesheet = parser.Parse(source, false);

            return true;// !string.IsNullOrWhiteSpace(stylesheet.Text);
        }

        protected override string UpdateLessSourceMapUrls(string content, string sourceFileName, string compiledFileName)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.LessSourceMaps))
                return content;

            string sourceMapFilename = compiledFileName + ".map";

            if (!File.Exists(sourceFileName) || !File.Exists(sourceMapFilename))
                return content;

            var updatedFileContent = GetUpdatedSourceMapFileContent(compiledFileName, sourceMapFilename);

            if (updatedFileContent == null)
                return content;

            WriteFile(updatedFileContent, sourceMapFilename, true, false);
            AddFileToProject(sourceFileName, sourceMapFilename);

            return UpdateSourceLinkInCssComment(content, FileHelpers.RelativePath(sourceMapFilename, compiledFileName));
        }

        private static string GetUpdatedSourceMapFileContent(string cssFileName, string sourceMapFilename)
        {
            // Read JSON map file and deserialize.
            dynamic jsonSourceMap = Json.Decode(File.ReadAllText(sourceMapFilename));

            if (jsonSourceMap == null)
                return null;

            jsonSourceMap.sources = ((IEnumerable<dynamic>)jsonSourceMap.sources).Select(s => FileHelpers.RelativePath(cssFileName, s));
            jsonSourceMap.names = new List<dynamic>(jsonSourceMap.names);
            jsonSourceMap.file = Path.GetFileName(cssFileName);

            return Json.Encode(jsonSourceMap);
        }

        private static string UpdateSourceLinkInCssComment(string content, string sourceMapRelativePath)
        {   // Fixed sourceMappingURL comment in CSS file with network accessible path.
            return _sourceMapinCSS.Replace(content,
                String.Format(CultureInfo.CurrentCulture, "/*# sourceMappingURL={0} */", sourceMapRelativePath));
        }
    }
}