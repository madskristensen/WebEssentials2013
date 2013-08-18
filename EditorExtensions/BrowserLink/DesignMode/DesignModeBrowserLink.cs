using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Web.BrowserLink;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(BrowserLinkExtensionFactory))]
    [BrowserLinkFactoryName("DesignMode")] // Not needed in final version of VS2013
    public class DesignModeFactory : BrowserLinkExtensionFactory
    {
        private static DesignMode _extension;

        public override BrowserLinkExtension CreateInstance(BrowserLinkConnection connection)
        {
            // Instantiate the extension as a singleton
            if (_extension == null)
            {
                _extension = new DesignMode();
            }

            return _extension;
        }

        public override string Script
        {
            get
            {
                using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.DesignMode.DesignModeBrowserLink.js"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    public class DesignMode : BrowserLinkExtension, IBrowserLinkActionProvider
    {
        BrowserLinkConnection _connection;

        public override void OnConnected(BrowserLinkConnection connection)
        {
            _connection = connection;
        }

        public IEnumerable<BrowserLinkAction> Actions
        {
            get { yield return new BrowserLinkAction("Design Mode", SetDesignMode); }
        }

        private void SetDesignMode()
        {
            Clients.Call(_connection, "setDesignMode");
        }

        [BrowserLinkCallback]
        public void UpdateSource(string innerHTML, string file, int position)
        {
            EditorExtensionsPackage.DTE.ItemOperations.OpenFile(file);

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                var view = ProjectHelpers.GetCurentTextView();
                var html = HtmlEditorDocument.FromTextView(view);

                view.Selection.Clear();
                ElementNode element;
                AttributeNode attribute;

                html.HtmlEditorTree.GetPositionElement(position + 1, out element, out attribute);

                if (element != null && element.EndTag != null && element.InnerRange.Start <= element.InnerRange.End)
                {
                    Span span = new Span(element.InnerRange.Start, element.InnerRange.Length);
                    string text = html.TextBuffer.CurrentSnapshot.GetText(span);

                    if (text != innerHTML)
                    {
                        try
                        {
                            EditorExtensionsPackage.DTE.UndoContext.Open("Design Mode changes");
                            html.TextBuffer.Replace(span, innerHTML);
                            EditorExtensionsPackage.DTE.ActiveDocument.Save();
                            //html.HtmlEditorTree.RequestFullParse();                            
                        }
                        catch
                        {
                            // Do nothing
                        }
                        finally
                        {
                            EditorExtensionsPackage.DTE.UndoContext.Close();
                        }
                    }
                }

            }), DispatcherPriority.ApplicationIdle, null);
        }

        [BrowserLinkCallback]
        public void Undo()
        {
            try
            {
                EditorExtensionsPackage.ExecuteCommand("Edit.Undo");
                EditorExtensionsPackage.DTE.ActiveDocument.Save();
            }
            catch
            {
                // Do nothing
            }
        }

        [BrowserLinkCallback]
        public void Redo()
        {
            try
            {
                EditorExtensionsPackage.ExecuteCommand("Edit.Redo");
                EditorExtensionsPackage.DTE.ActiveDocument.Save();
            }
            catch
            {
                // Do nothing
            }
        }

        [BrowserLinkCallback]
        public void Save()
        {
            if (EditorExtensionsPackage.DTE.ActiveDocument != null && !EditorExtensionsPackage.DTE.ActiveDocument.Saved)
            {
                EditorExtensionsPackage.DTE.ActiveDocument.Save();
            }
        }
    }
}