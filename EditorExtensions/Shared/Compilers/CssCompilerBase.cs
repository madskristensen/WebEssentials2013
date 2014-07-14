using System;
using System.IO;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public abstract class CssCompilerBase : NodeExecutorBase
    {
        protected override string PostProcessResult(CompilerResult result)
        {
            string resultString = result.Result;

            // If the caller wants us to renormalize URLs to a different filename, do so.
            if (result.TargetFileName != null &&
                Path.GetDirectoryName(result.TargetFileName) != Path.GetDirectoryName(result.SourceFileName) &&
                result.Result.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
            {
                try
                {
                    resultString = CssUrlNormalizer.NormalizeUrls(
                        tree: new CssParser().Parse(result.Result, true),
                        targetFile: result.TargetFileName,
                        oldBasePath: result.SourceFileName
                    );

                    Logger.Log(ServiceName + ": " + Path.GetFileName(result.SourceFileName) + " compiled.");
                }
                catch (Exception ex)
                {
                    Logger.Log(ServiceName + ": An error occurred while normalizing generated paths in " + result.SourceFileName + "\r\n" + ex);
                }
            }
            else
            {
                Logger.Log(ServiceName + ": " + Path.GetFileName(result.SourceFileName) + " compiled.");
            }

            return resultString;
        }
    }
}
