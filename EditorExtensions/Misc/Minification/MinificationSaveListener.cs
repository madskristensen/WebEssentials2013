using System;
using System.ComponentModel.Composition;
using System.IO;
using MadsKristensen.EditorExtensions.Commands;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Optimization.Minification
{
    [Export]    // This class is imported for its own methods, and as a general save listener
    [Export(typeof(IFileSaveListener))]
    [ContentType("HTMLX")]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    class MinificationSaveListener : IFileSaveListener
    {
        public void FileSaved(IContentType contentType, string path, bool forceSave, bool minifyInPlace)
        {
            // This will also be called for derived ContentTypes like LESS & Markdown.  Ignore those.
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);

            if (settings == null || !settings.AutoMinify)
                return;

            if (minifyInPlace)
                MinifyFile(contentType, path, path, settings);
            else
                ReMinify(contentType, path, forceSave, settings);
        }

        ///<summary>Minifies an existing file if it should be minified.</summary>
        public static void ReMinify(IContentType contentType, string path, bool forceSave, IMinifierSettings settings = null)
        {
            // Don't minify ".min" files
            if (!ShouldMinify(path))
                return;

            string minPath = GetMinFileName(path);

            if (!forceSave && !File.Exists(minPath))
                return;

            MinifyFile(contentType, path, minPath, settings ?? WESettings.Instance.ForContentType<IMinifierSettings>(contentType));
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

        public static void CreateMinFile(IContentType contentType, string sourcePath)
        {
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            var minPath = GetMinFileName(sourcePath);

            MinifyFile(contentType, sourcePath, minPath, settings);

            if (settings.GzipMinifiedFiles)
                ProjectHelpers.AddFileToProject(minPath, minPath + ".gzip");
        }

        private static void MinifyFile(IContentType contentType, string sourcePath, string minPath, IMinifierSettings settings)
        {
            IFileMinifier minifier = Mef.GetImport<IFileMinifier>(contentType);
            bool changed = minifier.MinifyFile(sourcePath, minPath);

            if (settings.GzipMinifiedFiles && (changed || !File.Exists(minPath + ".gzip")))
                FileHelpers.GzipFile(minPath);
        }
    }
}
