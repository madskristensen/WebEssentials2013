using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class DesignModeFactory : IBrowserLinkExtensionFactory
    {
        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            return new DesignMode();
        }

        public string GetScript()
        {
            using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.DesignMode.DesignModeBrowserLink.js"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class DesignMode : BrowserLinkExtension
    {
        BrowserLinkConnection _connection;

        public override IEnumerable<BrowserLinkAction> Actions
        {
            get { yield return new BrowserLinkAction("Design Mode", SetDesignMode); }
        }

        public override void OnConnected(BrowserLinkConnection connection)
        {
            _connection = connection;
        }

        private void SetDesignMode(BrowserLinkAction action)
        {
            Browsers.Client(_connection).Invoke("setDesignMode");
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [BrowserLinkCallback]
        public void UpdateSource(string innerHtml, string file, int position)
        {
            WebEssentialsPackage.DTE.ItemOperations.OpenFile(file);

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                var view = ProjectHelpers.GetCurentTextView();
                var html = HtmlEditorDocument.FromTextView(view);
                ElementNode element;
                AttributeNode attribute;

                view.Selection.Clear();
                html.HtmlEditorTree.GetPositionElement(position + 1, out element, out attribute);

                // HTML element
                if (element != null && element.Start == position)
                {
                    Span span = new Span(element.InnerRange.Start, element.InnerRange.Length);
                    string text = html.TextBuffer.CurrentSnapshot.GetText(span);

                    if (text != innerHtml)
                    {
                        UpdateBuffer(innerHtml, html, span);
                    }
                }
                // ActionLink
                else if (element.Start != position)
                {
                    //@Html.ActionLink("Application name", "Index", "Home", null, new { @class = "brand" })
                    Span span = new Span(position, 100);

                    if (position + 100 < html.TextBuffer.CurrentSnapshot.Length)
                    {
                        string text = html.TextBuffer.CurrentSnapshot.GetText(span);
                        var result = Regex.Replace(text, @"^html.actionlink\(""([^""]+)""", "Html.ActionLink(\"" + innerHtml + "\"", RegexOptions.IgnoreCase);

                        UpdateBuffer(result, html, span);
                    }
                }

            }), DispatcherPriority.ApplicationIdle, null);
        }

        private static void UpdateBuffer(string innerHTML, HtmlEditorDocument html, Span span)
        {
            using (WebEssentialsPackage.UndoContext("Design Mode changes"))
            {
                try
                {
                    html.TextBuffer.Replace(span, innerHTML);
                    WebEssentialsPackage.DTE.ActiveDocument.Save();
                }
                catch
                {
                    // Do nothing
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [BrowserLinkCallback]
        public void Undo()
        {
            try
            {
                WebEssentialsPackage.ExecuteCommand("Edit.Undo");
                WebEssentialsPackage.DTE.ActiveDocument.Save();
            }
            catch
            {
                // Do nothing
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [BrowserLinkCallback]
        public void Redo()
        {
            try
            {
                WebEssentialsPackage.ExecuteCommand("Edit.Redo");
                WebEssentialsPackage.DTE.ActiveDocument.Save();
            }
            catch
            {
                // Do nothing
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [BrowserLinkCallback]
        public void Save()
        {
            if (WebEssentialsPackage.DTE.ActiveDocument != null && !WebEssentialsPackage.DTE.ActiveDocument.Saved)
            {
                WebEssentialsPackage.DTE.ActiveDocument.Save();
            }
        }
    }
}