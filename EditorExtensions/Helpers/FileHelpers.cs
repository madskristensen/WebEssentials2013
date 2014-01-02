using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EnvDTE;
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
        ///<summary>Gets the currently selected span within a specific buffer type, or null if there is no selection or if the selection is in a different buffer.</summary>
        ///<param name="view">The TextView containing the selection</param>
        ///<param name="contentType">The ContentType to filter the selection by.</param>        
        public static SnapshotPoint? GetSelection(this ITextView view, string contentType)
        {
            return view.BufferGraph.MapDownToInsertionPoint(view.Caret.Position.BufferPosition, PointTrackingMode.Positive, ts => ts.ContentType.IsOfType(contentType));
        }
        ///<summary>Gets the currently selected span within a specific buffer type, or null if there is no selection or if the selection is in a different buffer.</summary>
        ///<param name="view">The TextView containing the selection</param>
        ///<param name="contentTypes">The ContentTypes to filter the selection by.</param>        
        public static SnapshotPoint? GetSelection(this ITextView view, params string[] contentTypes)
        {
            return view.BufferGraph.MapDownToInsertionPoint(view.Caret.Position.BufferPosition, PointTrackingMode.Positive, ts => contentTypes.Any(c => ts.ContentType.IsOfType(c)));
        }
        ///<summary>Gets the currently selected span within a specific buffer type, or null if there is no selection or if the selection is in a different buffer.</summary>
        ///<param name="view">The TextView containing the selection</param>
        ///<param name="contentTypeFilter">The ContentType to filter the selection by.</param>        
        public static SnapshotPoint? GetSelection(this ITextView view, Func<IContentType, bool> contentTypeFilter)
        {
            return view.BufferGraph.MapDownToInsertionPoint(view.Caret.Position.BufferPosition, PointTrackingMode.Positive, ts => contentTypeFilter(ts.ContentType));
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

        public static string ShowDialog(string extension)
        {
            var initialPath = Path.GetDirectoryName(EditorExtensionsPackage.DTE.ActiveDocument.FullName);

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = "file." + extension;
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
                    return "image/jpeg" + extension;
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

        public static string GetMimeTypeFromBase64(string base64)
        {
            int end = base64.IndexOf(";", StringComparison.Ordinal);

            if (end > -1)
            {
                return base64.Substring(5, end - 5);
            }

            return string.Empty;
        }

        public static string ConvertToBase64(string fileName)
        {
            string format = "data:{0};base64,{1}";
            byte[] buffer = File.ReadAllBytes(fileName);
            string extension = Path.GetExtension(fileName).Substring(1);
            string contentType = GetMimeTypeFromFileExtension(extension);

            return string.Format(CultureInfo.InvariantCulture, format, contentType, Convert.ToBase64String(buffer));
        }

        static char[] pathSplit = { '/', '\\' };
        public static string RelativePath(string absolutePath, string relativeTo)
        {
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

        internal static bool CanCompile(string fileName, string compileToExtension)
        {
            if (ProjectHelpers.GetProjectItem(fileName) == null)
                return false;

            if (Path.GetFileName(fileName).StartsWith("_", StringComparison.Ordinal))
                return false;

            string minFile = MarginBase.GetCompiledFileName(fileName, ".min" + compileToExtension, WESettings.GetString(WESettings.Keys.CoffeeScriptCompileToLocation));

            if (File.Exists(minFile) && WESettings.GetBoolean(WESettings.Keys.CoffeeScriptMinify))
                return true;

            string jsFile = MarginBase.GetCompiledFileName(fileName, compileToExtension, WESettings.GetString(WESettings.Keys.CoffeeScriptCompileToLocation));

            if (!File.Exists(jsFile))
                return false;

            return true;
        }

        internal static void WriteResult(CompilerResult result, string compileToFileName, string compileToExtension)
        {
            MinifyFile(result.FileName, result.Result, compileToExtension);

            if (!File.Exists(compileToFileName))
                return;

            string old = File.ReadAllText(compileToFileName);

            if (old == result.Result)
                return;

            ProjectHelpers.CheckOutFileFromSourceControl(compileToFileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(compileToFileName, false, new UTF8Encoding(true)))
                {
                    writer.Write(result.Result);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        internal static void MinifyFile(string sourceFileName, string source, string compileToExtension)
        {
            if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptMinify))
            {
                string content = MinifyFileMenu.MinifyString(compileToExtension, source);
                string minFile = MarginBase.GetCompiledFileName(sourceFileName, ".min" + compileToExtension, WESettings.GetString(WESettings.Keys.CoffeeScriptCompileToLocation));
                bool fileExist = File.Exists(minFile);
                string old = fileExist ? File.ReadAllText(minFile) : string.Empty;

                if (old != content)
                {
                    ProjectHelpers.CheckOutFileFromSourceControl(minFile);

                    using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(true)))
                    {
                        writer.Write(content);
                    }

                    if (!fileExist)
                        AddFileToProject(sourceFileName, minFile);
                }
            }
        }

        internal static bool WriteFile(string content, string fileName)
        {
            bool fileWritten = false;

            try
            {
                if (File.Exists(fileName))
                {
                    using (StreamWriter writer = new StreamWriter(fileName, false, new UTF8Encoding(true)))
                    {
                        writer.Write(content);
                        fileWritten = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ShowMessage("Could not write to " + Path.GetFileName(fileName) +
                                    Environment.NewLine + ex.Message);
            }

            return fileWritten;
        }

        public static ProjectItem AddFileToProject(string parentFileName, string fileName)
        {
            if (!File.Exists(fileName))
                return null;

            var item = ProjectHelpers.GetProjectItem(parentFileName);

            if (item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName))
            {
                if (item.ContainingProject.GetType().Name != "OAProject" && item.ProjectItems != null && Path.GetDirectoryName(parentFileName) == Path.GetDirectoryName(fileName))
                {   // WAP
                    return item.ProjectItems.AddFromFile(fileName);
                }
                else
                {   // Website
                    return item.ContainingProject.ProjectItems.AddFromFile(fileName);
                }
            }

            return null;
        }
    }
}
