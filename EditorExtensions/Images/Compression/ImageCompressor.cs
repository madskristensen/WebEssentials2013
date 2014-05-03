using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;

namespace MadsKristensen.EditorExtensions.Images
{
    internal class ImageCompressor
    {
        private const string _dataUriPrefix = "base64-";

        public static bool IsFileSupported(string fileName)
        {
            return GetArguments(fileName, string.Empty) != null;
        }

        public async Task<string> CompressDataUriAsync(string dataUri)
        {
            string mimeType = FileHelpers.GetMimeTypeFromBase64(dataUri);
            string extension = FileHelpers.GetExtension(mimeType);

            if (!IsFileSupported("file." + extension))
                return dataUri;

            string temp = Path.Combine(Path.GetTempPath(), _dataUriPrefix + Guid.NewGuid() + "." + extension);
            bool isFileSaved = await FileHelpers.SaveDataUriToFile(dataUri, temp);

            if (isFileSaved)
            {
                await CompressFilesAsync(temp);
                string base64 = await FileHelpers.ConvertToBase64(temp);
                File.Delete(temp);

                return base64;
            }

            return dataUri;
        }

        public async Task CompressFilesAsync(params string[] fileNames)
        {
            EditorExtensionsPackage.DTE.StatusBar.Text = fileNames.Length == 1 ? "Optimizing " + Path.GetFileName(fileNames[0]) + "..." : "Optimizing " + fileNames.Length + " images...";
            EditorExtensionsPackage.DTE.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationDeploy);
            List<CompressionResult> list = new List<CompressionResult>();

            try
            {
                await Task.WhenAll(fileNames.Select(async file =>
                {
                    if (!File.Exists(file))
                        return;

                    var result = await CompressFile(file);

                    if (result.Saving > 0)
                        list.Add(result);

                    HandleResult(file, result);
                }));

                if (fileNames.Length > 1)
                    DisplayEndResult(list);
            }
            catch
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = "The image could not be optimized. Wrong format";
            }
            finally
            {
                EditorExtensionsPackage.DTE.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationDeploy);
            }
        }

        private static void DisplayEndResult(List<CompressionResult> list)
        {
            long savings = list.Sum(r => r.Saving);
            long originals = list.Sum(r => r.OriginalFileSize);
            long results = list.Sum(r => r.ResultFileSize);

            if (savings > 0)
            {
                double percent = 100 - Math.Round((double)results / (double)originals * 100, 1);
                EditorExtensionsPackage.DTE.StatusBar.Text = list.Count + " images optimized. Total saving of " + savings + " bytes / " + percent + "%";
            }
            else
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = "The images were already optimized";
            }
        }

        private static void HandleResult(string file, CompressionResult result)
        {
            string name = file.Contains(_dataUriPrefix) ? "the dataUri" : Path.GetFileName(file);

            if (result.Saving > 0)
            {
                ProjectHelpers.CheckOutFileFromSourceControl(result.OriginalFileName);
                File.Copy(result.ResultFileName, result.OriginalFileName, true);

                string text = "Compressed " + name + " by " + result.Saving + " bytes / " + result.Percent + "%";
                EditorExtensionsPackage.DTE.StatusBar.Text = text;
                Logger.Log(result.ToString());
            }
            else
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = name + " is already optimized";
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
                CreateNoWindow = true,
            };

            try
            {
                using (var process = await start.ExecuteAsync())
                {
                    return new CompressionResult(fileName, targetFile);
                }
            }
            catch
            {
                CompressionResult result = new CompressionResult(fileName, targetFile);
                File.Delete(targetFile);
                return result;
            }
        }

        private static string GetArguments(string sourceFile, string targetFile)
        {
            if (!Uri.IsWellFormedUriString(sourceFile, UriKind.RelativeOrAbsolute) && !File.Exists(sourceFile))
                return null;

            string ext;

            try
            {
                ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            }
            catch (ArgumentException)
            {
                return null;
            }

            switch (ext)
            {
                case ".png":
                    return string.Format(CultureInfo.CurrentCulture, "/c png.cmd \"{0}\" \"{1}\"", sourceFile, targetFile);

                case ".jpg":
                case ".jpeg":
                    return string.Format(CultureInfo.CurrentCulture, "/c jpegtran -copy none -optimize -progressive \"{0}\" \"{1}\"", sourceFile, targetFile);

                case ".gif":
                    return string.Format(CultureInfo.CurrentCulture, "/c gifsicle --crop-transparency --no-comments --no-extensions --no-names --optimize=3 --batch \"{0}\" --output=\"{1}\"", sourceFile, targetFile);
            }

            return null;
        }
    }
}