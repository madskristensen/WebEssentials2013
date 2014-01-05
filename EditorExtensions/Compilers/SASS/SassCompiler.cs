using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public class SassCompiler : NodeExecutorBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\node_modules\node-sass\bin\node-sass");
        private static readonly Regex _errorParsingPattern = new Regex(@"(?<fileName>.*):(?<line>.\d*): error: (?<message>.*\n.*)", RegexOptions.Multiline);
        private static readonly Regex _sourceMapInCss = new Regex(@"\/\*#([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*\/", RegexOptions.Multiline);

        protected override string ServiceName
        {
            get { return "SASS"; }
        }
        protected override string CompilerPath
        {
            get { return _compilerPath; }
        }
        protected override Regex ErrorParsingPattern
        {
            get { return _errorParsingPattern; }
        }
        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            var args = new StringBuilder("--output-style=expanded ");

            if (WESettings.GetBoolean(WESettings.Keys.SassSourceMaps) && !InUnitTests)
            {
                args.Append("--source-comments=map ");
            }

            args.AppendFormat(CultureInfo.CurrentCulture, "\"{0}\" \"{1}\"", sourceFileName, targetFileName);

            return args.ToString();
        }

        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            // Inserts an empty row between each rule and replace two space indentation with 4 space indentation
            resultSource = UpdateSourceMapUrls(resultSource, targetFileName);

            var message = "SASS: " + Path.GetFileName(sourceFileName) + " compiled.";

            // If the caller wants us to renormalize URLs to a different filename, do so.
            if (targetFileName != null && resultSource.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
            {
                try
                {
                    resultSource = CssUrlNormalizer.NormalizeUrls(
                        tree: new CssParser().Parse(resultSource, true),
                        targetFile: targetFileName,
                        oldBasePath: sourceFileName
                    );
                }
                catch (Exception ex)
                {
                    message = "SASS: An error occurred while normalizing generated paths in " + sourceFileName + "\r\n" + ex;
                }
            }

            Logger.Log(message);

            return resultSource;
        }

        private static string UpdateSourceMapUrls(string content, string compiledFileName)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.SassSourceMaps) || !File.Exists(compiledFileName))
                return content;

            string sourceMapFilename = compiledFileName + ".map";

            if (!File.Exists(sourceMapFilename))
                return content;

            var updatedFileContent = GetUpdatedSourceMapFileContent(compiledFileName, sourceMapFilename);

            if (updatedFileContent == null)
                return content;

            FileHelpers.WriteFile(updatedFileContent, sourceMapFilename);
            ProjectHelpers.AddFileToProject(compiledFileName, sourceMapFilename);

            return UpdateSourceLinkInCssComment(content, FileHelpers.RelativePath(compiledFileName, sourceMapFilename));
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
        {   // Fix sourceMappingURL comment in CSS file with network accessible path.
            return _sourceMapInCss.Replace(content,
                string.Format(CultureInfo.CurrentCulture, "/*# sourceMappingURL={0} */", sourceMapRelativePath));
        }
    }
}