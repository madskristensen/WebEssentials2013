using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Autoprefixer;
using MadsKristensen.EditorExtensions.Helpers;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Newtonsoft.Json.Linq;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>A base class for a compiler that rewrites CSS source maps.</summary>
    public abstract class CssCompilerBase : NodeExecutorBase
    {
        private static readonly Regex _sourceMapInCss = new Regex(@"\/\*#.*(?i:sourceMappingURL)([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*\/", RegexOptions.Multiline);

        protected async override Task<string> PostProcessResult(string resultSource, string sourceFileName, string targetFileName, string mapFileName)
        {
            string newResult = await UpdateSourceMapUrls(resultSource, targetFileName, mapFileName);

            string message = ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.";

            if (WESettings.Instance.Css.Autoprefix)
            {
                if (!ReferenceEquals(string.Intern(newResult), string.Intern(resultSource)))
                    await FileHelpers.WriteAllTextRetry(targetFileName, newResult);

                string autoprefixResult = await CssAutoprefixer.AutoprefixFile(sourceFileName, targetFileName, mapFileName);

                if (autoprefixResult != null)
                    newResult = await UpdateSourceMapUrls(autoprefixResult, targetFileName, mapFileName);
            }

            // If the caller wants us to renormalize URLs to a different filename, do so.
            if (targetFileName != null && Path.GetDirectoryName(targetFileName) != Path.GetDirectoryName(sourceFileName)
             && newResult.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
            {
                try
                {
                    newResult = CssUrlNormalizer.NormalizeUrls(
                        tree: new CssParser().Parse(newResult, true),
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

            return newResult;
        }

        private async Task<string> UpdateSourceMapUrls(string content, string compiledFileName, string mapFileName)
        {
            if (!File.Exists(compiledFileName) || !File.Exists(mapFileName))
                return content;

            var updatedFileContent = await GetUpdatedSourceMapFileContent(compiledFileName, mapFileName);

            if (updatedFileContent == null)
                return content;

            if (!ReferenceEquals(string.Intern(updatedFileContent), string.Intern(await FileHelpers.ReadAllTextRetry(mapFileName))))
                await FileHelpers.WriteAllTextRetry(mapFileName, updatedFileContent, false);

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
            string jsonString = await ReadMapFile(sourceMapFileName);

            if (string.IsNullOrEmpty(jsonString))
                return null;

            // Read JSON map file and deserialize.

            JObject jsonSourceMap = JObject.Parse(jsonString);

            if (jsonSourceMap == null)
                return null;

            for (int i = 0; i < ((JArray)jsonSourceMap["sources"]).Count; i++)
            {
                jsonSourceMap["sources"][i] = FileHelpers.RelativePath(cssFileName, jsonSourceMap["sources"][i].Value<string>());
            }

            jsonSourceMap["file"] = Path.GetFileName(cssFileName);

            return jsonSourceMap.ToString();
        }

        private static string UpdateSourceLinkInCssComment(string content, string sourceMapRelativePath)
        {   // Fix sourceMappingURL comment in CSS file with network accessible path.
            return _sourceMapInCss.Replace(content,
                   string.Format(CultureInfo.InvariantCulture,
                   "/*# sourceMappingURL={0} */", sourceMapRelativePath));
        }
    }
}
