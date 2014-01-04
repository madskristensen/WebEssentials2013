using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows.Forms;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IDropHandlerProvider))]
    [DropFormat("FileDrop")]
    [DropFormat("CF_VSSTGPROJECTITEMS")]
    [Name("FontDropHandler")]
    [ContentType("CSS")]
    [ContentType("LESS")]
    [Order(Before = "DefaultFileDropHandler")]
    internal class FontDropHandlerProvider : IDropHandlerProvider
    {
        public IDropHandler GetAssociatedDropHandler(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty<FontDropHandler>(() => new FontDropHandler(view));
        }
    }

    internal class FontDropHandler : IDropHandler
    {
        IWpfTextView view;
        private readonly Dictionary<string, string> formats = new Dictionary<string, string>() 
        {
            {".ttf", " format('truetype')"},
            {".woff", ""},
            {".eot", ""},
            {".otf", " format('opentype')"}
        };
        private string draggedFilename;
        private string fontName = string.Empty;
        string fontFace = "@font-face {{\n\tfont-family: {0};\n\tsrc: {1};\n}}";
        string fontUrls = "url('{0}'){1}";
        //ITextDocument document;

        public FontDropHandler(IWpfTextView view)
        {
            this.view = view;
            //view.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document);
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            if (File.Exists(draggedFilename))
            {
                //var files = GetRelativeFiles(draggedFilename);
                //string[] sources = new string[files.Count()];

                //for (int i = 0; i < files.Count(); i++)
                //{
                //    string file = files.ElementAt(i);
                //    string extension = Path.GetExtension(file).ToLowerInvariant();
                //    string reference = RelativePath(document.FilePath, file);

                //    if (reference.StartsWith("http://localhost:"))
                //    {
                //        int index = reference.IndexOf('/', 24);
                //        if (index > -1)
                //            reference = reference.Substring(index + 1).ToLowerInvariant();
                //    }

                //    sources[i] = string.Format(fontUrls, reference, formats[extension]);
                //}

                //string sourceUrls = string.Join(", ", sources);
                string fontFamily;
                view.TextBuffer.Insert(dragDropInfo.VirtualBufferPosition.Position.Position, GetCodeFromFile(draggedFilename, out fontFamily));

                return DragDropPointerEffects.Copy;
            }
            else if (draggedFilename.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase))
            {
                //int index = draggedFilename.IndexOf('/', 24);
                //if (index > -1)
                //    draggedFilename = draggedFilename.Substring(index).ToLowerInvariant();

                //string extension = Path.GetExtension(draggedFilename).ToLowerInvariant();
                //string sourceUrl = string.Format(fontUrls, draggedFilename, formats[extension]);

                view.TextBuffer.Insert(dragDropInfo.VirtualBufferPosition.Position.Position, GetCodeFromLocalhost());

                return DragDropPointerEffects.Copy;
            }
            else
            {
                return DragDropPointerEffects.None;
            }
        }

        public string GetCodeFromFile(string fileName, out string fontFamily)
        {
            var files = GetRelativeFiles(fileName);
            string[] sources = new string[files.Count()];

            for (int i = 0; i < files.Count(); i++)
            {
                string file = files.ElementAt(i);
                string extension = Path.GetExtension(file).ToLowerInvariant();
                string reference = FileHelpers.RelativePath(EditorExtensionsPackage.DTE.ActiveDocument.FullName, file);

                if (reference.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase))
                {
                    int index = reference.IndexOf('/', 24);
                    if (index > -1)
                        reference = reference.Substring(index + 1).ToLowerInvariant();
                }

                sources[i] = string.Format(CultureInfo.CurrentCulture, fontUrls, reference, formats[extension]);
            }

            string sourceUrls = string.Join(", ", sources);
            fontFamily = fontName;
            fontFamily = HttpUtility.UrlPathEncode(fontFamily);
            return string.Format(CultureInfo.CurrentCulture, fontFace, fontName, sourceUrls);
        }

        private string GetCodeFromLocalhost()
        {
            int index = draggedFilename.IndexOf('/', 24);
            if (index > -1)
                draggedFilename = draggedFilename.Substring(index).ToLowerInvariant();

            string extension = Path.GetExtension(draggedFilename).ToLowerInvariant();
            draggedFilename = HttpUtility.UrlPathEncode(draggedFilename);
            string sourceUrl = string.Format(CultureInfo.CurrentCulture, fontUrls, draggedFilename, formats[extension]);

            return string.Format(CultureInfo.CurrentCulture, fontFace, "MyFontName", sourceUrl);
        }

        private IEnumerable<string> GetRelativeFiles(string fileName)
        {
            var fi = new FileInfo(fileName);
            fontName = fi.Name.Replace(fi.Extension, string.Empty);
            foreach (var file in fi.Directory.GetFiles(fontName + ".*"))
            {
                string extension = file.Extension.ToLowerInvariant();
                if (formats.ContainsKey(extension))
                    yield return file.FullName;
            }
        }

        public void HandleDragCanceled()
        {
        }

        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            //if (!Path.GetExtension(document.FilePath).Equals(".css", StringComparison.OrdinalIgnoreCase))
            //    return false;

            draggedFilename = GetImageFilename(dragDropInfo);

            if (!string.IsNullOrEmpty(draggedFilename))
            {
                string fileExtension = Path.GetExtension(draggedFilename).ToLowerInvariant();
                if (this.formats.ContainsKey(fileExtension))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetImageFilename(DragDropInfo info)
        {
            DataObject data = new DataObject(info.Data);

            if (info.Data.GetDataPresent("FileDrop"))
            {
                // The drag and drop operation came from the file system
                StringCollection files = data.GetFileDropList();

                if (files != null && files.Count == 1)
                {
                    return files[0];
                }
            }
            else if (info.Data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
            {
                // The drag and drop operation came from the VS solution explorer
                return data.GetText();
            }

            return null;
        }

        //public static string RelativePath(string absPath, string relTo)
        //{
        //    string[] absDirs = absPath.Split('\\');
        //    string[] relDirs = relTo.Split('\\');

        //    // Get the shortest of the two paths
        //    int len = absDirs.Length < relDirs.Length ? absDirs.Length :
        //    relDirs.Length;

        //    // Use to determine where in the loop we exited
        //    int lastCommonRoot = -1;
        //    int index;

        //    // Find common root
        //    for (index = 0; index < len; index++)
        //    {
        //        if (absDirs[index] == relDirs[index]) lastCommonRoot = index;
        //        else break;
        //    }

        //    // If we didn't find a common prefix then throw
        //    if (lastCommonRoot == -1)
        //    {
        //        return relTo;
        //    }

        //    // Build up the relative path
        //    StringBuilder relativePath = new StringBuilder();

        //    // Add on the ..
        //    for (index = lastCommonRoot + 2; index < absDirs.Length; index++)
        //    {
        //        if (absDirs[index].Length > 0) relativePath.Append("..\\");
        //    }

        //    // Add on the folders
        //    for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
        //    {
        //        relativePath.Append(relDirs[index] + "\\");
        //    }
        //    relativePath.Append(relDirs[relDirs.Length - 1]);

        //    return relativePath.ToString().Replace("\\", "/");
        //}
    }
}
