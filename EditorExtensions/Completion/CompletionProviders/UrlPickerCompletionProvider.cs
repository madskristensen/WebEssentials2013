using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("UrlPickerCompletionProvider")]
    internal class UrlPickerCompletionProvider : ICssCompletionListProvider, ICssCompletionCommitListener
    {
        private static List<string> _imageExtensions = new List<string>() { "", ".png", ".jpg", "gif", ".svg", ".jpeg", ".bmp", ".tif", ".tiff" };
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)604; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            UrlItem urlItem = (UrlItem)context.ContextItem;

            string url = urlItem.UrlString != null ? urlItem.UrlString.Text : string.Empty;
            string directory = GetDirectory(url);

            if (url.StartsWith("http") || url.Contains("//") || url.Contains(";base64,") || !Directory.Exists(directory))
                yield break;

            foreach (string item in Directory.GetFileSystemEntries(directory))
            {
                string entry = item.Substring(item.LastIndexOf("\\") + 1);

                //if (_imageExtensions.Contains(Path.GetExtension(entry)))
                    yield return new UrlPickerCompletionListEntry(entry);
            }
        }

        private static string GetDirectory(string url)
        {
            if (url == "/" || url.LastIndexOf('/') == 0)
                return GetRootFolder();

            return GetRelativeFolder(url);
        }

        private static string GetRelativeFolder(string url)
        {
            int end = Math.Max(0, url.LastIndexOf('/'));
            return ProjectHelpers.ToAbsoluteFilePath(url.Substring(0, end));
        }

        private static string GetRootFolder()
        {
            string root = ProjectHelpers.GetRootFolder();
            if (File.Exists(root))
                return Path.GetDirectoryName(root);

            return root;
        }

        public void OnCommitted(ICssCompletionListEntry entry, Microsoft.VisualStudio.Text.ITrackingSpan contextSpan, Microsoft.VisualStudio.Text.SnapshotPoint caret, Microsoft.VisualStudio.Text.Editor.ITextView textView)
        {
            if (Path.GetExtension(entry.DisplayText).Length == 0)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => CssCompletionController.FromView(textView).OnShowMemberList(filterList: true)), DispatcherPriority.Normal);
            }
        }
    }
}
