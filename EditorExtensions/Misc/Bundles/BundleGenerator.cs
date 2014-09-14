using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Helpers;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal static class BundleGenerator
    {
        public async static Task<bool> MakeBundle(BundleDocument document, string bundleFile, Func<string, bool, Task> updateBundle)
        {
            // filePath must end in ".targetExtension.bundle"
            string extension = Path.GetExtension(Path.GetFileNameWithoutExtension(document.FileName));

            if (string.IsNullOrEmpty(extension))
            {
                Logger.Log("Skipping bundle file " + document.FileName + " without extension.  Bundle files must end with the output extension, followed by '.bundle'.");
                return false;
            }

            Dictionary<string, string> files = await WatchFiles(document, updateBundle, bundleFile);

            string combinedContent = await CombineFiles(files, extension, document, bundleFile);
            bool bundleChanged = !File.Exists(bundleFile) || await FileHelpers.ReadAllTextRetry(bundleFile) != combinedContent;

            if (bundleChanged)
            {
                if (!ProjectHelpers.CheckOutFileFromSourceControl(bundleFile))
                    throw new Exception("There was a problem checking out the file: " + bundleFile);

                await FileHelpers.WriteAllTextRetry(bundleFile, combinedContent);
                Logger.Log("Web Essentials: Updated bundle: " + Path.GetFileName(bundleFile));
            }

            ProjectHelpers.AddFileToProject(document.FileName, bundleFile);

            return bundleChanged;
        }

        public async static Task<Dictionary<string, string>> WatchFiles(BundleDocument document, Func<string, bool, Task> updateBundle, string bundleFile = null)
        {
            if (document == null)
                return null;

            if (bundleFile == null && document.OutputDirectory != null)
                bundleFile = ProjectHelpers.GetAbsolutePathFromSettings(document.OutputDirectory, Path.Combine(Path.GetDirectoryName(document.FileName), Path.GetFileNameWithoutExtension(document.FileName)));

            Dictionary<string, string> files = new Dictionary<string, string>();

            await new BundleFileObserver().AttachFileObserver(document, document.FileName, updateBundle);

            foreach (string asset in document.OriginalBundleAssets)
            {
                string absolute = asset.Contains(":\\") ? asset : ProjectHelpers.ToAbsoluteFilePath(asset, document.FileName);

                if (!File.Exists(absolute))
                {
                    WebEssentialsPackage.DTE.ItemOperations.OpenFile(document.FileName);
                    Logger.ShowMessage(String.Format(CultureInfo.CurrentCulture, "Bundle error: The file '{0}' doesn't exist", absolute));

                    return null;
                }

                if (!files.ContainsKey(absolute))
                {
                    if (Path.IsPathRooted(asset))
                        files.Add(absolute, asset);
                    else
                        files.Add(absolute, FileHelpers.RelativePath(bundleFile, Path.GetFullPath(Path.Combine(Path.GetDirectoryName(document.FileName), asset))));

                    await new BundleFileObserver().AttachFileObserver(document, absolute, updateBundle);
                }
            }

            return files;
        }

        private async static Task<string> CombineFiles(Dictionary<string, string> files, string extension, BundleDocument bundle, string bundleFile)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string file in files.Keys)
            {
                string actualFile = file;

                if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) && WESettings.Instance.JavaScript.GenerateSourceMaps)
                {
                    sb.AppendLine("///#source 1 1 " + files[file]);
                }

                var source = await FileHelpers.ReadAllTextRetry(actualFile);

                if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
                {
                    // If the bundle is in the same folder as the CSS,
                    // or if does not have URLs, no need to normalize.
                    if (Path.GetDirectoryName(actualFile) != Path.GetDirectoryName(bundleFile) &&
                        source.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0 &&
                        bundle.AdjustRelativePaths)
                        source = CssUrlNormalizer.NormalizeUrls(
                            tree: new CssParser().Parse(source, true),
                            targetFile: bundleFile,
                            oldBasePath: actualFile
                        );
                }
                else if (Path.GetExtension(file).Equals(".ts", StringComparison.OrdinalIgnoreCase))
                {
                    // If it is a Type Script include, we might want to alter the define(... at the start of the script to specify the resource location.
                    if (source.StartsWith("define([\"require\", \"exports\""))
                    {
                        string moduleName = files[file];

                        moduleName = Path.Combine(Path.GetDirectoryName(moduleName), Path.GetFileNameWithoutExtension(moduleName)).Replace('\\', '/');

                        if (moduleName.ToLower().StartsWith(WESettings.Instance.JavaScript.RootDirectory.ToLower()))
                            moduleName = moduleName.Substring(WESettings.Instance.JavaScript.RootDirectory.Length);

                        source = source.Replace("define([\"require\", \"exports\"", "define(\"" + moduleName + "\", [\"require\", \"exports\"");
                    }
                }

                sb.AppendLine(source);
            }
            return sb.ToString();
        }

        public async static Task MakeMinFile(string bundleSourcePath, string extension, bool bundleChanged)
        {
            string minPath = Path.ChangeExtension(bundleSourcePath, ".min" + Path.GetExtension(bundleSourcePath));

            // If the bundle didn't change, don't re-minify, unless the user just enabled minification.
            if (!bundleChanged && File.Exists(minPath))
                return;

            var fers = WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>().Value;
            var contentType = fers.GetContentTypeForExtension(extension);
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            var minifier = Mef.GetImport<IFileMinifier>(contentType);
            bool changed = await minifier.MinifyFile(bundleSourcePath, minPath);

            if (settings.GzipMinifiedFiles && (changed || !File.Exists(minPath + ".gzip")))
                FileHelpers.GzipFile(minPath);
        }
    }
}

