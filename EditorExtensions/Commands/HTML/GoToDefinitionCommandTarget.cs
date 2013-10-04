﻿using Microsoft.CSS.Core;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    internal class HtmlGoToDefinition : CommandTargetBase
    {
        private HtmlEditorTree _tree;
        private string _path, _className;

        public HtmlGoToDefinition(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(Microsoft.VisualStudio.VSConstants.VSStd97CmdID).GUID, (uint)Microsoft.VisualStudio.VSConstants.VSStd97CmdID.GotoDefn)
        {
            _tree = HtmlEditorDocument.FromTextView(textView).HtmlEditorTree;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
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

                EditorExtensionsPackage.DTE.StatusBar.Text = "Couldn't find " + _path;
            }
            else if (!string.IsNullOrEmpty(_className))
            {
                int position;
                string file = FindFile(new[] { ".less", ".sass", ".css" }, out position);

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

                EditorExtensionsPackage.DTE.StatusBar.Text = "Couldn't find " + _className;
            }

            return false;
        }

        private string FindFile(IEnumerable<string> extensions, out int position)
        {
            LessParser parser = new LessParser();
            string root = ProjectHelpers.GetProjectFolder(TextView.TextBuffer.GetFileName());
            position = -1;

            foreach (string ext in extensions)
            {
                foreach (string file in Directory.GetFiles(root, "*" + ext, SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".min" + ext))
                        continue;

                    string text = File.ReadAllText(file);
                    int index = text.IndexOf("." + _className);

                    if (index > -1)
                    {
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

                        if (medium != null)
                        {
                            position = medium.Start;
                            return file;
                        }

                        var low = selectors.FirstOrDefault();
                        
                        if (low != null)
                        {
                            position = low.Start;
                            return file;
                        }
                    }
                }
            }

            return null;
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
            className = null;

            ElementNode element = null;
            AttributeNode attr = null;

            _tree.GetPositionElement(position, out element, out attr);

            if (attr == null || attr.Name != "class")
                return false;

            string value = attr.Value;
            int beginning = position - attr.ValueRangeUnquoted.Start;
            int start = attr.Value.LastIndexOf(' ', beginning) + 1;
            int length = attr.Value.IndexOf(' ', start) - start;

            if (length < 0)
                length = attr.ValueRangeUnquoted.Length - start;
            //281, 269, 10
            className = attr.Value.Substring(start, length);

            return true;
        }

        protected override bool IsEnabled()
        {
            return TryGetPath(out _path) || TryGetClassName(out _className);
        }
    }
}