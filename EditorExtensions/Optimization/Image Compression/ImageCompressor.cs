using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;

namespace MadsKristensen.EditorExtensions
{
    internal class ImageCompressor
    {
        public static bool IsFileSupported(string fileName)
        {
            return GetArguments(fileName, string.Empty) != null;
        }

        public async Task CompressFiles(params string[] fileNames)
        {
            EditorExtensionsPackage.DTE.StatusBar.Text = fileNames.Length == 1 ? "Optimizing " + Path.GetFileName(fileNames[0]) + "..." : "Optimizing " + fileNames.Length + " images...";
            EditorExtensionsPackage.DTE.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationDeploy);
            List<CompressionResult> list = new List<CompressionResult>();

            await Task.WhenAll(fileNames.Select(async file =>
            {
                if (!File.Exists(file))
                    return;

                var result = await CompressFile(file);

                if (result.Saving > 0)
                    list.Add(result);

                HandleResult(file, result);
            }));

            EditorExtensionsPackage.DTE.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationDeploy);

            if (fileNames.Length > 1)
                DisplayEndResult(list);
        }

        private static void DisplayEndResult(List<CompressionResult> list)
        {
            long savings = list.Sum(r => r.Saving);
            long originals = list.Sum(r => r.OriginalFileSize);
            long results = list.Sum(r => r.ResultFileSize);

            if (savings > 0)
            {
                double percent = Math.Round((double)results / (double)originals * 100, 1);
                EditorExtensionsPackage.DTE.StatusBar.Text = list.Count + " images optimized. Total saving of " + savings + " bytes / " + percent + "%";
            }
            else
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = "The images were already optimized";
            }
        }

        private static void HandleResult(string file, CompressionResult result)
        {
            if (result.Saving > 0)
            {
                ProjectHelpers.CheckOutFileFromSourceControl(result.OriginalFileName);
                File.Copy(result.ResultFileName, result.OriginalFileName, true);

                double percent = Math.Round((double)result.ResultFileSize / (double)result.OriginalFileSize * 100, 1);
                string text = "Compressed " + Path.GetFileName(file) + " by " + result.Saving + " bytes / " + percent + "%...";
                EditorExtensionsPackage.DTE.StatusBar.Text = text;
            }
            else
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = Path.GetFileName(file) + " is already optimized...";
            }
        }

        private async Task<CompressionResult> CompressFile(string fileName)
        {
            string targetFile = Path.ChangeExtension(Path.GetTempFileName(), Path.GetExtension(fileName));

            ProcessStartInfo start = new ProcessStartInfo("cmd")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), @"Resources\tools\"),
                Arguments = GetArguments(fileName, targetFile),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = await start.ExecuteAsync())
            {
                return new CompressionResult(fileName, targetFile);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        private static string GetArguments(string sourceFile, string targetFile)
        {
            string ext = Path.GetExtension(sourceFile).ToLowerInvariant();

            switch (ext)
            {
                case ".png":
                    return string.Format("/c png.cmd \"{0}\" \"{1}\"", sourceFile, targetFile);

                case ".jpg":
                case ".jpeg":
                    return string.Format("/c jpegtran -copy none -optimize -progressive \"{0}\" \"{1}\"", sourceFile, targetFile);
            }

            return null;
        }
    }
}