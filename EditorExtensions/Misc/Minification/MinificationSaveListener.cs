using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Commands;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Optimization.Minification
{
    [Export]    // This class is imported for its own methods, and as a general save listener
    [Export(typeof(IFileSaveListener))]
    [ContentType("HTMLX")]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    [ContentType("node.js")]
    class MinificationSaveListener : IFileSaveListener
    {
        public async Task FileSaved(IContentType contentType, string path, bool forceSave, bool minifyInPlace)
        {
            // This will also be called for derived ContentTypes like LESS & Markdown.  Ignore those.
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);

            if (settings == null || !settings.AutoMinify)
                return;

            if (minifyInPlace)
                await MinifyFile(contentType, path, path, settings, !minifyInPlace);
            else
                await ReMinify(contentType, path, forceSave, settings);
        }

        ///<summary>Minifies an existing file if it should be minified.</summary>
        public async static Task ReMinify(IContentType contentType, string path, bool forceSave, IMinifierSettings settings = null)
        {
            // Don't minify ".min" files
            if (!ShouldMinify(path))
                return;

            string minPath = GetMinFileName(path);

            if (!forceSave && !File.Exists(minPath))
                return;

            await MinifyFile(contentType, path, minPath, settings ?? WESettings.Instance.ForContentType<IMinifierSettings>(contentType));
        }

        public static string GetMinFileName(string path)
        {
            return path.Insert(path.Length - Path.GetExtension(path).Length, ".min");
        }

        public static bool ShouldMinify(string path)
        {
            var baseName = Path.GetFileNameWithoutExtension(path);
            return !baseName.EndsWith(".min", StringComparison.OrdinalIgnoreCase)
                && !baseName.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase);
        }

        public async static Task CreateMinFile(IContentType contentType, string sourcePath)
        {
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            var minPath = GetMinFileName(sourcePath);

            await MinifyFile(contentType, sourcePath, minPath, settings);

            if (settings.GzipMinifiedFiles)
                ProjectHelpers.AddFileToProject(minPath, minPath + ".gzip");
        }

        private async static Task MinifyFile(IContentType contentType, string sourcePath, string minPath, IMinifierSettings settings, bool compilerNeedsSourceMap = true)
        {
            IFileMinifier minifier = Mef.GetImport<IFileMinifier>(contentType);
            bool changed = await minifier.MinifyFile(sourcePath, minPath, compilerNeedsSourceMap);

            if (settings.GzipMinifiedFiles && (changed || !File.Exists(minPath + ".gzip")))
                FileHelpers.GzipFile(minPath);
        }
    }
}
