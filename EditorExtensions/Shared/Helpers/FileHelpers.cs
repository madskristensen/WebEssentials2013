using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public static class FileHelpers
    {
        public static SnapshotPoint? GetCurrentSelection(string contentType) { return ProjectHelpers.GetCurentTextView().GetSelection(contentType); }
        ///<summary>Gets the currently selected point within a specific buffer type, or null if there is no selection or if the selection is in a different buffer.</summary>
        ///<param name="view">The TextView containing the selection</param>
        ///<param name="contentType">The ContentType to filter the selection by.</param>        
        public static SnapshotPoint? GetSelection(this ITextView view, string contentType)
        {
            return view.BufferGraph.MapDownToInsertionPoint(view.Caret.Position.BufferPosition, PointTrackingMode.Positive, ts => ts.ContentType.IsOfType(contentType));
        }
        ///<summary>Gets the currently selected point within a specific buffer type, or null if there is no selection or if the selection is in a different buffer.</summary>
        ///<param name="view">The TextView containing the selection</param>
        ///<param name="contentTypes">The ContentTypes to filter the selection by.</param>        
        public static SnapshotPoint? GetSelection(this ITextView view, params string[] contentTypes)
        {
            return view.BufferGraph.MapDownToInsertionPoint(view.Caret.Position.BufferPosition, PointTrackingMode.Positive, ts => contentTypes.Any(c => ts.ContentType.IsOfType(c)));
        }
        ///<summary>Gets the currently selected point within a specific buffer type, or null if there is no selection or if the selection is in a different buffer.</summary>
        ///<param name="view">The TextView containing the selection</param>
        ///<param name="contentTypeFilter">The ContentType to filter the selection by.</param>        
        public static SnapshotPoint? GetSelection(this ITextView view, Func<IContentType, bool> contentTypeFilter)
        {
            return view.BufferGraph.MapDownToInsertionPoint(view.Caret.Position.BufferPosition, PointTrackingMode.Positive, ts => contentTypeFilter(ts.ContentType));
        }
        ///<summary>Gets the first currently selected span within a specific buffer type, or null if there is no selection or if the selection is in a different buffer.</summary>
        ///<param name="view">The TextView containing the selection</param>
        ///<param name="contentTypeFilter">The ContentType to filter the selection by.</param>        
        public static SnapshotSpan? GetSelectedSpan(this ITextView view, Func<IContentType, bool> contentTypeFilter)
        {
            return view.Selection.SelectedSpans.SelectMany(span =>
                view.BufferGraph.MapDownToFirstMatch(
                    span,
                    SpanTrackingMode.EdgePositive,
                    ts => contentTypeFilter(ts.ContentType)
                ), (s, c) => (SnapshotSpan?)s
            ).FirstOrDefault();
        }

        public static void GzipFile(string sourcePath)
        {
            var gzipPath = sourcePath + ".gzip";
            ProjectHelpers.CheckOutFileFromSourceControl(gzipPath);

            using (var sourceStream = File.OpenRead(sourcePath))
            using (var targetStream = File.OpenWrite(gzipPath))
            using (var gzipStream = new GZipStream(targetStream, CompressionMode.Compress))
                sourceStream.CopyTo(gzipStream);

            ProjectHelpers.AddFileToProject(sourcePath, gzipPath);
        }

        public static void OpenFileInPreviewTab(string file)
        {
            IVsNewDocumentStateContext newDocumentStateContext = null;

            try
            {
                IVsUIShellOpenDocument3 openDoc3 = EditorExtensionsPackage.GetGlobalService<SVsUIShellOpenDocument>() as IVsUIShellOpenDocument3;

                Guid reason = VSConstants.NewDocumentStateReason.Navigation;
                newDocumentStateContext = openDoc3.SetNewDocumentState((uint)__VSNEWDOCUMENTSTATE.NDS_Provisional, ref reason);

                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(file);
            }
            finally
            {
                if (newDocumentStateContext != null)
                    newDocumentStateContext.Restore();
            }
        }

        public static string ShowDialog(string extension, string fileName = "file.")
        {
            var initialPath = Path.GetDirectoryName(EditorExtensionsPackage.DTE.ActiveDocument.FullName);

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = fileName + extension;
                dialog.DefaultExt = extension;
                dialog.Filter = extension.ToUpperInvariant() + " files | *." + extension;
                dialog.InitialDirectory = initialPath;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }

            return null;
        }

        public static string GetExtension(string mimeType)
        {
            switch (mimeType)
            {
                case "image/png":
                    return "png";

                case "image/jpg":
                case "image/jpeg":
                    return "jpg";

                case "image/gif":
                    return "gif";

                case "image/svg":
                    return "svg";

                case "font/x-woff":
                    return "woff";

                case "font/otf":
                    return "otf";

                case "application/vnd.ms-fontobject":
                    return "eot";

                case "application/octet-stream":
                    return "ttf";
            }

            return null;
        }

        private static string GetMimeTypeFromFileExtension(string extension)
        {
            string ext = extension.TrimStart('.');

            switch (ext)
            {
                case "jpg":
                case "jpeg":
                    return "image/jpeg";
                case "svg":
                    return "image/svg+xml";
                case "png":
                case "gif":
                case "tiff":
                case "webp":
                case "bmp":
                    return "image/" + ext;

                case "woff":
                    return "font/x-woff";

                case "otf":
                    return "font/otf";

                case "eot":
                    return "application/vnd.ms-fontobject";

                case "ttf":
                    return "application/octet-stream";

                default:
                    return "text/plain";
            }
        }

        public async static Task<bool> SaveDataUriToFile(string dataUri, string filePath)
        {
            try
            {
                int index = dataUri.IndexOf("base64,", StringComparison.Ordinal) + 7;
                byte[] imageBytes = Convert.FromBase64String(dataUri.Substring(index));
                await FileHelpers.WriteAllBytesRetry(filePath, imageBytes);
                ProjectHelpers.AddFileToActiveProject(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.ShowMessage(ex.Message, "Web Essentials " + ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static string GetMimeTypeFromBase64(string base64)
        {
            int end = base64.IndexOf(';');

            if (end > -1)
            {
                return base64.Substring(5, end - 5);
            }

            return string.Empty;
        }

        public async static Task<string> ConvertToBase64(string fileName)
        {
            if (!File.Exists(fileName))
                return string.Empty;

            string format = "data:{0};base64,{1}";
            byte[] buffer = await FileHelpers.ReadAllBytesRetry(fileName);
            string extension = Path.GetExtension(fileName).Substring(1);
            string contentType = GetMimeTypeFromFileExtension(extension);

            return string.Format(CultureInfo.InvariantCulture, format, contentType, Convert.ToBase64String(buffer));
        }

        static char[] pathSplit = { '/', '\\' };

        public static string RelativePath(string absolutePath, string relativeTo)
        {
            relativeTo = relativeTo.Replace("\\/", "\\");

            string[] absDirs = absolutePath.Split(pathSplit);
            string[] relDirs = relativeTo.Split(pathSplit);

            // Get the shortest of the two paths
            int len = Math.Min(absDirs.Length, relDirs.Length);

            // Use to determine where in the loop we exited
            int lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < len; index++)
            {
                if (absDirs[index].Equals(relDirs[index], StringComparison.OrdinalIgnoreCase)) lastCommonRoot = index;
                else break;
            }

            // If we didn't find a common prefix then throw
            if (lastCommonRoot == -1)
            {
                return relativeTo;
            }

            // Build up the relative path
            StringBuilder relativePath = new StringBuilder();

            // Add on the ..
            for (index = lastCommonRoot + 2; index < absDirs.Length; index++)
            {
                if (absDirs[index].Length > 0) relativePath.Append("..\\");
            }

            // Add on the folders
            for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
            {
                relativePath.Append(relDirs[index] + "\\");
            }
            relativePath.Append(relDirs[relDirs.Length - 1]);

            return relativePath.Replace('\\', '/').ToString();
        }

        public static void SearchFiles(string term, string fileTypes)
        {
            Find2 find = (Find2)EditorExtensionsPackage.DTE.Find;
            string types = find.FilesOfType;
            bool matchCase = find.MatchCase;
            bool matchWord = find.MatchWholeWord;

            find.WaitForFindToComplete = false;
            find.Action = EnvDTE.vsFindAction.vsFindActionFindAll;
            find.Backwards = false;
            find.MatchInHiddenText = true;
            find.MatchWholeWord = true;
            find.MatchCase = true;
            find.PatternSyntax = EnvDTE.vsFindPatternSyntax.vsFindPatternSyntaxLiteral;
            find.ResultsLocation = EnvDTE.vsFindResultsLocation.vsFindResults1;
            find.SearchSubfolders = true;
            find.FilesOfType = fileTypes;
            find.Target = EnvDTE.vsFindTarget.vsFindTargetSolution;
            find.FindWhat = term;
            find.Execute();

            find.FilesOfType = types;
            find.MatchCase = matchCase;
            find.MatchWholeWord = matchWord;
        }

        /// <summary>
        /// Opens a text file,
        /// tries reading file 5 times before throwing IO Exception,
        /// and then closes the file.
        /// </summary>
        /// <param name="fileName">The file to open for reading.</param>
        /// <returns>Task which ultimately returns a string containing all lines of the file.</returns>
        public async static Task<string> ReadAllTextRetry(string fileName)
        {
            int retryCount = 500;

            try
            {
                return await Task.FromResult<string>(File.ReadAllText(fileName))
                            .ExecuteRetryableTaskAsync<string>(PolicyFactory.GetPolicy(new FileTransientErrorDetectionStrategy(), retryCount));
            }
            catch (IOException)
            {
                Logger.Log("Exception: Tried " + retryCount + " times for reading, but the file " + fileName + " is still in use. Exiting gracefully.");
            }

            return string.Empty;
        }

        /// <summary>
        /// Tries reading the lines of a file 5 times before throwing IO Exception.
        /// </summary>
        /// <param name="fileName">The file to open for reading.</param>
        /// <returns>Task which ultimately returns all lines of the file, or the lines that are the result of a query.</returns>
        public async static Task<IEnumerable<string>> ReadAllLinesRetry(string fileName)
        {
            int retryCount = 500;

            try
            {
                return await Task.FromResult<IEnumerable<string>>(File.ReadLines(fileName))
                            .ExecuteRetryableTaskAsync<IEnumerable<string>>(PolicyFactory.GetPolicy(new FileTransientErrorDetectionStrategy(), retryCount));
            }
            catch (IOException)
            {
                Logger.Log("Exception: Tried " + retryCount + " times for reading, but the file " + fileName + " is still in use. Exiting gracefully.");
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Opens a text file,
        /// tries reading file into a byte array 5 times before throwing IO Exception,
        /// and then closes the file.
        /// </summary>
        /// <param name="fileName">The file to open for reading.</param>
        /// <returns>Task which ultimately returns all lines of the file, or the lines that are the result of a query.</returns>
        public async static Task<byte[]> ReadAllBytesRetry(string fileName)
        {
            int retryCount = 500;

            try
            {
                return await Task.FromResult<byte[]>(File.ReadAllBytes(fileName))
                            .ExecuteRetryableTaskAsync<byte[]>(PolicyFactory.GetPolicy(new FileTransientErrorDetectionStrategy(), retryCount));
            }
            catch (IOException)
            {
                Logger.Log("Exception: Tried " + retryCount + " times for reading, but the file " + fileName + " is still in use. Exiting gracefully.");
            }

            return Enumerable.Empty<byte>().ToArray();
        }

        /// <summary>
        /// Creates a new file, writes the specified string to the file, and then closes
        /// the file. If the target file already exists, it is overwritten. If the target
        /// file is in use, try 5 times before throwing IO Exception.
        /// </summary>
        /// <param name="fileName">The file to open for reading.</param>
        /// <param name="contents">The string to write to the file.</param>
        public async static Task WriteAllTextRetry(string fileName, string contents)
        {
            int retryCount = 500;

            try
            {
                await Task.Run(() => File.WriteAllText(fileName, contents, Encoding.UTF8))
                     .ExecuteRetryableTaskAsync(PolicyFactory.GetPolicy(new FileTransientErrorDetectionStrategy(), retryCount));
            }
            catch (IOException)
            {
                Logger.Log("Exception: Tried " + retryCount + " times for writing, but the file " + fileName + " is still in use. Exiting gracefully.");
            }
        }

        /// <summary>
        /// Creates a new file, writes the specified byte array to the file, and then closes
        /// the file. If the target file already exists, it is overwritten. If the target
        /// file is in use, try 5 times before throwing IO Exception.
        /// </summary>
        /// <param name="fileName">The file to open for reading.</param>
        /// <param name="value">The bytes to write to the file.</param>
        public async static Task WriteAllBytesRetry(string fileName, byte[] value)
        {
            int retryCount = 500;

            try
            {
                await Task.Run(() => File.WriteAllBytes(fileName, value))
                     .ExecuteRetryableTaskAsync(PolicyFactory.GetPolicy(new FileTransientErrorDetectionStrategy(), retryCount));
            }
            catch (IOException)
            {
                Logger.Log("Exception: Tried" + retryCount + " times for writing, but the file " + fileName + " is still in use. Exiting gracefully.");
            }
        }

        /// <summary>
        ///    Returns the file name of the specified path string without the extension.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>
        ///    The string returned by System.IO.Path.GetFileName(System.String), minus the
        //     first period (.) and all characters following it.
        /// </returns>
        public static string GetFileNameWithoutExtension(string path)
        {
            var fileNameWithoutPath = Path.GetFileName(path);

            return Path.GetFileNameWithoutExtension(fileNameWithoutPath).Substring(0, fileNameWithoutPath.IndexOf('.'));
        }

        /// <summary>
        /// Gets the file name collisions.
        /// </summary>
        /// <param name="fileName">Name of the file to check.</param>
        /// <param name="extensions">The extensions to append to the file name to also check.</param>
        /// <returns>The colliding file name if there is one, else <see langword="null"/>.</returns>
        public static string GetFileCollisions(string fileName, params string[] extensions)
        {
            return File.Exists(fileName)
                 ? fileName
                 : extensions.Select(extension => fileName + extension).FirstOrDefault(File.Exists);
        }
    }
}
