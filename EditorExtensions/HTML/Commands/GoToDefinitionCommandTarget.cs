using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    internal class HtmlGoToDefinition : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        private HtmlEditorTree _tree;
        private string _path, _className;

        public HtmlGoToDefinition(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd97CmdID.GotoDefn)
        {
            _tree = HtmlEditorDocument.FromTextView(textView).HtmlEditorTree;
        }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!string.IsNullOrEmpty(_path))
            {
                _path = _path.TrimStart('~').Trim();
                string absolute = ProjectHelpers.ToAbsoluteFilePathFromActiveFile(_path);

                if (File.Exists(absolute))
                {
                    FileHelpers.OpenFileInPreviewTab(absolute);
                    return true;
                }

                WebEssentialsPackage.DTE.StatusBar.Text = "Couldn't find " + _path;
            }
            else if (!string.IsNullOrEmpty(_className))
            {
                int position;
                string file = FindFile(new[] { ".less", ".scss", ".css" }, out position);

                if (!string.IsNullOrEmpty(file))
                {
                    FileHelpers.OpenFileInPreviewTab(file);

                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            IWpfTextView view = ProjectHelpers.GetCurentTextView();
                            ITextSnapshot snapshot = view.TextBuffer.CurrentSnapshot;
                            view.Caret.MoveTo(new SnapshotPoint(snapshot, position));
                            view.ViewScroller.EnsureSpanVisible(new SnapshotSpan(snapshot, position, 1), EnsureSpanVisibleOptions.AlwaysCenter);
                        }
                        catch
                        { }

                    }), DispatcherPriority.ApplicationIdle, null);

                    return true;
                }

                WebEssentialsPackage.DTE.StatusBar.Text = "Couldn't find " + _className;
            }

            return false;
        }

        private string FindFile(IEnumerable<string> extensions, out int position)
        {
            string root = ProjectHelpers.GetProjectFolder(TextView.TextBuffer.GetFileName());
            position = -1;
            bool isLow = false, isMedium = false;
            string result = null;

            foreach (string ext in extensions)
            {
                ICssParser parser = CssParserLocator.FindComponent(Mef.GetContentType(ext.Trim('.'))).CreateParser();

                foreach (string file in Directory.EnumerateFiles(root, "*" + ext, SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".min" + ext, StringComparison.OrdinalIgnoreCase) ||
                        file.Contains("node_modules") ||
                        file.Contains("bower_components"))
                        continue;

                    string text = FileHelpers.ReadAllTextRetry(file).ConfigureAwait(false).GetAwaiter().GetResult();
                    int index = text.IndexOf("." + _className, StringComparison.Ordinal);

                    if (index == -1)
                        continue;

                    var css = parser.Parse(text, true);
                    var visitor = new CssItemCollector<ClassSelector>(false);
                    css.Accept(visitor);

                    var selectors = visitor.Items.Where(c => c.ClassName.Text == _className);
                    var high = selectors.FirstOrDefault(c => c.FindType<AtDirective>() == null && (c.Parent.NextSibling == null || c.Parent.NextSibling.Text == ","));

                    if (high != null)
                    {
                        position = high.Start;
                        return file;
                    }

                    var medium = selectors.FirstOrDefault(c => c.Parent.NextSibling == null || c.Parent.NextSibling.Text == ",");

                    if (medium != null && !isMedium)
                    {
                        position = medium.Start;
                        result = file;
                        isMedium = true;
                        continue;
                    }

                    var low = selectors.FirstOrDefault();

                    if (low != null && !isMedium && !isLow)
                    {
                        position = low.Start;
                        result = file;
                        isLow = true;
                        continue;
                    }
                }
            }

            return result;
        }

        private bool TryGetPath(out string path)
        {
            int position = TextView.Caret.Position.BufferPosition.Position;
            path = null;

            ElementNode element = null;
            AttributeNode attr = null;

            _tree.GetPositionElement(position, out element, out attr);

            if (element == null)
                return false;

            attr = element.GetAttribute("src") ?? element.GetAttribute("href");

            if (attr != null)
            {
                path = attr.Value;
                return true;
            }

            return false;
        }

        private bool TryGetClassName(out string className)
        {
            int position = TextView.Caret.Position.BufferPosition.Position;
            className = HtmlHelpers.GetSinglePropertyValue(_tree, position, "class");

            return !string.IsNullOrEmpty(className);
        }

        protected override bool IsEnabled()
        {
            return TryGetPath(out _path) || TryGetClassName(out _className);
        }
    }
}