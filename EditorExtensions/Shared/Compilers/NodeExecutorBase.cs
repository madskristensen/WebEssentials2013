using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.JavaScript;
using MadsKristensen.EditorExtensions.Settings;

namespace MadsKristensen.EditorExtensions
{
    public abstract class NodeExecutorBase
    {
        protected virtual bool Previewing { get { return false; } }

        ///<summary>Indicates whether this compiler will emit a source map file.  Will only return true if supported and enabled in user settings.</summary>
        public abstract bool MinifyInPlace { get; }
        public abstract bool GenerateSourceMap { get; }
        public abstract string TargetExtension { get; }
        public abstract string ServiceName { get; }

        public async Task<CompilerResult> CompileAsync(string sourceFileName, string targetFileName)
        {
            bool onlyPreview = false;

            if (WEIgnore.TestWEIgnore(sourceFileName, this is ILintCompiler ? "linter" : "compiler", ServiceName.ToLowerInvariant()))
            {
                if (WESettings.Instance.General.ShowWEIgnoreLogs)
                    Logger.Log(String.Format(CultureInfo.CurrentCulture,
                                            "{0}: The file {1} is ignored by .weignore. Skipping..",
                                             ServiceName, Path.GetFileName(sourceFileName)));

                if (!Previewing)
                    return await CompilerResultFactory.GenerateResult(sourceFileName, targetFileName, string.Empty, false, string.Empty, string.Empty, Enumerable.Empty<CompilerError>(), true);

                onlyPreview = true;
            }

            CompilerResult response = await NodeServer.CallServiceAsync(GetPath(sourceFileName, targetFileName));

            return await ProcessResult(response, onlyPreview, sourceFileName, targetFileName);
        }

        // Don't try-catch this method: We need to "address" all the bugs,
        // which may occur as the (node.js-based) service implement changes.
        private async Task<CompilerResult> ProcessResult(CompilerResult result, bool onlyPreview, string sourceFileName, string targetFileName)
        {
            if (result == null)
            {
                Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compilation failed: The service failed to respond to this request\n\t\t\tPossible cause: Syntax Error!");
                return await CompilerResultFactory.GenerateResult(sourceFileName, targetFileName);
            }
            if (!result.IsSuccess)
            {
                var firstError = result.Errors.Where(e => e != null).Select(e => e.Message).FirstOrDefault();

                if (firstError != null)
                    Logger.Log(firstError);

                return result;
            }

            string resultString = PostProcessResult(result.Result, result.TargetFileName, result.SourceFileName);

            if (!onlyPreview)
            {
                // Write output file
                if (result.TargetFileName != null && (MinifyInPlace || !File.Exists(result.TargetFileName) ||
                    resultString != await FileHelpers.ReadAllTextRetry(result.TargetFileName)))
                {
                    ProjectHelpers.CheckOutFileFromSourceControl(result.TargetFileName);
                    await FileHelpers.WriteAllTextRetry(result.TargetFileName, resultString);

                    if (!(this is ILintCompiler))
                        ProjectHelpers.AddFileToProject(result.SourceFileName, result.TargetFileName);
                }

                // Write map file
                if (GenerateSourceMap && (!File.Exists(result.MapFileName) ||
                    result.ResultMap != await FileHelpers.ReadAllTextRetry(result.MapFileName)))
                {
                    ProjectHelpers.CheckOutFileFromSourceControl(result.MapFileName);
                    await FileHelpers.WriteAllTextRetry(result.MapFileName, result.ResultMap);

                    if (!(this is ILintCompiler))
                        ProjectHelpers.AddFileToProject(result.TargetFileName, result.MapFileName);
                }

                await RtlVariantHandler(result);
            }

            return CompilerResult.UpdateResult(result, resultString);
        }

        public static string GetOrCreateGlobalSettings(string fileName)
        {
            string globalFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fileName);

            if (!File.Exists(globalFile))
            {
                string extensionDir = Path.GetDirectoryName(typeof(NodeExecutorBase).Assembly.Location);
                string settingsFile = Path.Combine(extensionDir, @"Resources\settings-defaults\", fileName);
                File.Copy(settingsFile, globalFile);
            }

            return globalFile;
        }

        protected virtual Task RtlVariantHandler(CompilerResult result)
        {
            return Task.Factory.StartNew(() => { });
        }

        protected virtual string PostProcessResult(string result, string targetFileName, string sourceFileName)
        {
            Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");
            return result;
        }

        protected abstract string GetPath(string sourceFileName, string targetFileName);
    }
}
