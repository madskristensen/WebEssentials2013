using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    public class LessMargin : MarginBase
    {
        public const string MarginName = "LessMargin";

        public LessMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        { }

        protected override async void StartCompiler(string source)
        {
            string lessFilePath = Document.FilePath;

            if (!CompileEnabled)
                return;

            string cssFilename = GetCompiledFileName(lessFilePath, ".css", CompileEnabled ? CompileToLocation : null);// Document.FilePath.Replace(".less", ".css");

            if (IsFirstRun && File.Exists(cssFilename))
            {
                OnCompilationDone(File.ReadAllText(cssFilename), lessFilePath);
                return;
            }

            Logger.Log("LESS: Compiling " + Path.GetFileName(lessFilePath));

            var result = await LessCompiler.Compile(lessFilePath, cssFilename, Path.GetDirectoryName(lessFilePath));

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
                string content = MinifyFileMenu.MinifyString(".css", source);
                string minFile = GetCompiledFileName(fileName, ".min.css", CompileToLocation);// fileName.Replace(".less", ".min.css");
                bool fileExist = File.Exists(minFile);

                ProjectHelpers.CheckOutFileFromSourceControl(minFile);

                using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(true)))
                {
                    writer.Write(content);
                }

                if (!fileExist)
                    AddFileToProject(Document.FilePath, minFile);
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

        protected override string UpdateLessSourceMapUrls(string content, string oldFileName, string newFileName)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.LessSourceMaps))
                return content;
            dynamic jsonSourceMap = null;
            string sourceMapFilename = oldFileName + ".map";
            // Read JSON map file and deserialize.
            string sourceMapContents = File.ReadAllText(sourceMapFilename);

            jsonSourceMap = Json.Decode(sourceMapContents);

            if (jsonSourceMap == null)
                return content;

            string projectRoot = ProjectHelpers.GetRootFolder(ProjectHelpers.GetActiveProject());
            string cssNetworkPath = FileHelpers.RelativePath(oldFileName, newFileName);
            string sourceMapRelativePath = FileHelpers.RelativePath(oldFileName, sourceMapFilename);

            jsonSourceMap.sources = new List<dynamic>(jsonSourceMap.sources);
            jsonSourceMap.names = new List<dynamic>(jsonSourceMap.names);
            jsonSourceMap.file = cssNetworkPath;

            for (int i = 0; i < jsonSourceMap.sources.Count; ++i)
            {
                jsonSourceMap.sources[i] = FileHelpers.RelativePath(newFileName, projectRoot + jsonSourceMap.sources[i]);
            }

            WriteFile(Json.Encode(jsonSourceMap), sourceMapFilename, File.Exists(sourceMapFilename), false);

            // Fixed sourceMappingURL comment in CSS file with network accessible path.
            return Regex.Replace(content, @"\/\*#([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*\/", "/*# sourceMappingURL=" + sourceMapRelativePath + "*/");
        }
    }
}