using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Commands;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Optimization.Minification
{
    [Export(typeof(IFileSaveListener))]
    [ContentType("HTMLX")]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    class MinificationSaveListener : IFileSaveListener
    {
        public void FileSaved(IContentType contentType, string path)
        {
            // Don't minify ".min" files
            if (Path.ChangeExtension(path, "").EndsWith(".min", StringComparison.OrdinalIgnoreCase))
                return;
            if (!File.Exists(GetMinFileName(path)))
                return;

            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            if (!settings.AutoMinify)
                return;

            MinifyFile(contentType, path, settings);
        }
        public static string GetMinFileName(string path)
        {
            return path.Insert(path.Length - Path.GetExtension(path).Length, ".min");
        }

        public void CreateMinFile(IContentType contentType, string sourcePath)
        {
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            MinifyFile(contentType, sourcePath, settings);

            var minPath = GetMinFileName(sourcePath);

            //TODO: Should be unnecessary: ProjectHelpers.AddFileToActiveProject(minPath);
            ProjectHelpers.AddFileToProject(sourcePath, minPath);
            if (settings.GzipMinifiedFiles)
                ProjectHelpers.AddFileToProject(sourcePath, minPath + ".gzip");
        }

        private void MinifyFile(IContentType contentType, string sourcePath, IMinifierSettings settings)
        {
            var minifier = Mef.GetImport<IFileMinifier>(contentType);

            var minPath = GetMinFileName(sourcePath);
            var minContent = minifier.MinifyFile(sourcePath, minPath);

            if (settings.GzipMinifiedFiles)
                GzipFile(minPath, minContent);
        }

        private static void GzipFile(string minPath, string content)
        {
            var gzipPath = minPath + ".gzip";
            ProjectHelpers.CheckOutFileFromSourceControl(gzipPath);

            using (var sourceStream = File.OpenRead(minPath))
            using (var targetStream = File.OpenWrite(gzipPath))
            using (var gzipStream = new GZipStream(targetStream, CompressionMode.Compress))
                sourceStream.CopyTo(targetStream);
        }
    }
}
