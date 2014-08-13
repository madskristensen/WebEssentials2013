using System;
using System.IO;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Helpers;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public abstract class CssCompilerBase : NodeExecutorBase
    {
        protected override string PostProcessResult(string result, string targetFileName, string sourceFileName)
        {
            // If the caller wants us to renormalize URLs to a different filename, do so.
            if (targetFileName != null &&
                Path.GetDirectoryName(targetFileName) != Path.GetDirectoryName(sourceFileName) &&
                result.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
            {
                try
                {
                    result = CssUrlNormalizer.NormalizeUrls(
                                   tree: new CssParser().Parse(result, true),
                                   targetFile: targetFileName,
                                   oldBasePath: sourceFileName);

                    Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");
                }
                catch (Exception ex)
                {
                    Logger.Log(ServiceName + ": An error occurred while normalizing generated paths in " + sourceFileName + "\r\n" + ex);
                }
            }
            else
            {
                Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");
            }

            return result;
        }

        protected async override Task RtlVariantHandler(CompilerResult result)
        {
            if (!WESettings.Instance.Css.RtlCss || result.RtlTargetFileName == null)
                return;

            string value = PostProcessResult(result.RtlResult, result.RtlTargetFileName, result.RtlSourceFileName);

            // Write output file
            if (result.RtlTargetFileName != null && (MinifyInPlace || !File.Exists(result.RtlTargetFileName) ||
                value != await FileHelpers.ReadAllTextRetry(result.RtlTargetFileName)))
            {
                ProjectHelpers.CheckOutFileFromSourceControl(result.RtlTargetFileName);
                await FileHelpers.WriteAllTextRetry(result.RtlTargetFileName, value);
                ProjectHelpers.AddFileToProject(result.RtlSourceFileName, result.RtlTargetFileName);
            }

            // Write map file
            if (GenerateSourceMap && (!File.Exists(result.RtlMapFileName) ||
                result.RtlResultMap != await FileHelpers.ReadAllTextRetry(result.RtlMapFileName)))
            {
                ProjectHelpers.CheckOutFileFromSourceControl(result.RtlMapFileName);
                await FileHelpers.WriteAllTextRetry(result.RtlMapFileName, result.RtlResultMap);
                ProjectHelpers.AddFileToProject(result.RtlTargetFileName, result.RtlMapFileName);
            }
        }
    }
}
