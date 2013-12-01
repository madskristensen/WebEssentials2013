using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("UrlPickerCompletionProvider")]
    internal class UrlPickerCompletionProvider : ICssCompletionListProvider, ICssCompletionCommitListener
    {
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)604; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            UrlItem urlItem = (UrlItem)context.ContextItem;

            string url = urlItem.UrlString != null ? urlItem.UrlString.Text : string.Empty;
            if (url.StartsWith("http") || url.Contains("//") || url.Contains(";base64,"))
                yield break;
            string directory = GetDirectory(url);

            if (!Directory.Exists(directory))
                yield break;

            foreach (FileSystemInfo item in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                //if (_imageExtensions.Contains(item.Extension))
                yield return new UrlPickerCompletionListEntry(item);
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
            return ProjectHelpers.ToAbsoluteFilePathFromActiveFile(url.Substring(0, end));
        }

        private static string GetRootFolder()
        {
            string root = ProjectHelpers.GetProjectFolder(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
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
