using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.JSON
{
    /// <summary>
    /// Inserts missing double quotes around JSON property names
    /// </summary>
    internal class FormatCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        public FormatCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.FORMATDOCUMENT)
        { }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var selection = TextView.GetSelection("JSON");

            if (selection == null)
                return false;

            var doc = JSONEditorDocument.FromTextBuffer(selection.Value.Snapshot.TextBuffer);

            if (doc == null)
                return false;

            var visitor = new JSONItemCollector<JSONMember>(true);
            doc.JSONDocument.Accept(visitor);

            var properties = visitor.Items.Where(i => !i.IsValid && i.Name != null).Reverse();

            if (properties.Count() == 0)
                return false;

            try
            {
                InsertMissingQuotes(properties);
            }
            catch
            {
                // Do nothing
            }

            return false;
        }

        private void InsertMissingQuotes(IEnumerable<JSONMember> properties)
        {
            using (WebEssentialsPackage.UndoContext("Inserting missing quotes"))
            {
                var edit = TextView.TextBuffer.CreateEdit();

                foreach (var prop in properties)
                {
                    string text = prop.Name.Text;

                    if (!text.EndsWith("\"", StringComparison.Ordinal) && !text.StartsWith("\"", StringComparison.Ordinal))
                        edit.Replace(prop.Name.Start, prop.Name.Length, "\"" + prop.Name.Text + "\"");
                }

                edit.Apply();
            }
        }

        protected override bool IsEnabled()
        {
            return TextView.GetSelection("JSON").HasValue;
        }
    }
}