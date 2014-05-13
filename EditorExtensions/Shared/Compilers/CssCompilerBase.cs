using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Helpers;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>A base class for a compiler that rewrites CSS source maps.</summary>
    public abstract class CssCompilerBase : NodeExecutorBase
    {
        private static readonly Regex _sourceMapInCss = new Regex(@"\/\*#([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*\/", RegexOptions.Multiline);

        protected async override Task<string> PostProcessResult(string resultSource, string sourceFileName, string targetFileName, string mapFileName)
        {
            resultSource = await UpdateSourceMapUrls(resultSource, targetFileName, mapFileName);

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


        private async Task<string> UpdateSourceMapUrls(string content, string compiledFileName, string mapFileName)
        {
            if (!File.Exists(compiledFileName) || !File.Exists(mapFileName))
                return content;

            var updatedFileContent = await GetUpdatedSourceMapFileContent(compiledFileName, mapFileName);

            if (updatedFileContent == null)
                return content;

            await FileHelpers.WriteAllTextRetry(mapFileName, updatedFileContent);

            if (!GenerateSourceMap)
                return _sourceMapInCss.Replace(content, string.Empty);

            return UpdateSourceLinkInCssComment(content, FileHelpers.RelativePath(compiledFileName, mapFileName));
        }

        protected override string GetMapFileName(string sourceFileName, string targetFileName)
        {
            var mapFileName = targetFileName + ".map";

            return GenerateSourceMap ? mapFileName : Path.Combine(Path.GetTempPath(), Path.GetFileName(mapFileName));
        }

        // Overridden to work around SASS bug
        // TODO: Remove when https://github.com/hcatlin/libsass/issues/242 is fixed
        protected async virtual Task<string> ReadMapFile(string sourceMapFileName) { return await FileHelpers.ReadAllTextRetry(sourceMapFileName); }

        private async Task<string> GetUpdatedSourceMapFileContent(string cssFileName, string sourceMapFileName)
        {
            // Read JSON map file and deserialize.
            dynamic jsonSourceMap = Json.Decode(await ReadMapFile(sourceMapFileName));

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
                   string.Format(CultureInfo.InvariantCulture,
                   "/*# sourceMappingURL={0} */", sourceMapRelativePath));
        }
    }
}
