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

namespace MadsKristensen.EditorExtensions.Compilers
{
    ///<summary>A base class for a compiler that rewrites CSS source maps.</summary>
    public abstract class CssCompilerBase : NodeExecutorBase
    {
        private static readonly Regex _sourceMapInCss = new Regex(@"\/\*#([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*\/", RegexOptions.Multiline);


        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            // Inserts an empty row between each rule and replace two space indentation with 4 space indentation
            resultSource = UpdateSourceMapUrls(resultSource, targetFileName);

            var message = ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.";

            // If the caller wants us to renormalize URLs to a different filename, do so.
            if (targetFileName != null && Path.GetDirectoryName(targetFileName) != Path.GetDirectoryName(sourceFileName)
             && resultSource.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
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
                    message = ServiceName + ": An error occurred while normalizing generated paths in " + sourceFileName + "\r\n" + ex;
                }
            }

            Logger.Log(message);

            return resultSource;
        }


        private string UpdateSourceMapUrls(string content, string compiledFileName)
        {
            if (!GenerateSourceMap || !File.Exists(compiledFileName))
                return content;

            string sourceMapFilename = compiledFileName + ".map";

            if (!File.Exists(sourceMapFilename))
                return content;

            var updatedFileContent = GetUpdatedSourceMapFileContent(compiledFileName, sourceMapFilename);

            if (updatedFileContent == null)
                return content;

            File.WriteAllText(sourceMapFilename, updatedFileContent, Encoding.UTF8);

            return UpdateSourceLinkInCssComment(content, FileHelpers.RelativePath(compiledFileName, sourceMapFilename));
        }

        // Overridden to work around SASS bug
        // TODO: Remove when https://github.com/hcatlin/libsass/issues/242 is fixed
        protected virtual string ReadMapFile(string sourceMapFileName) { return File.ReadAllText(sourceMapFileName); }

        private string GetUpdatedSourceMapFileContent(string cssFileName, string sourceMapFileName)
        {
            // Read JSON map file and deserialize.
            dynamic jsonSourceMap = Json.Decode(ReadMapFile(sourceMapFileName));

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
                string.Format(CultureInfo.InvariantCulture, "/*# sourceMappingURL={0} */", sourceMapRelativePath));
        }
    }
}
