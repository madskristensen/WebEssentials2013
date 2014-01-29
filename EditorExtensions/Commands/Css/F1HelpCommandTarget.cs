using System;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class F1Help : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        public F1Help(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd97CmdID.F1Help)
        { }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var selection = TextView.GetSelection("css");
            if (selection == null)
                return false;
            var doc = CssEditorDocument.FromTextBuffer(selection.Value.Snapshot.TextBuffer);
            ParseItem item = doc.StyleSheet.ItemBeforePosition(selection.Value);

            if (item == null)
                return false;

            return SchemaLookup(item, selection.Value.Snapshot.TextBuffer);
        }

        private delegate ICssCompletionListEntry Reference(string name);

        private static bool SchemaLookup(ParseItem item, ITextBuffer buffer)
        {
            if (item is ClassSelector || item is IdSelector || item is ItemName || item.Parent is RuleBlock || item.Parent is StyleSheet)
                return false;

            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaRootForBuffer(buffer);

            Declaration dec = item.FindType<Declaration>();
            if (dec != null && dec.PropertyName != null)
                return OpenReferenceUrl(schema.GetProperty, dec.PropertyName.Text, "http://realworldvalidator.com/css/properties/");

            PseudoClassFunctionSelector pseudoClassFunction = item.FindType<PseudoClassFunctionSelector>();
            if (pseudoClassFunction != null)
                return OpenReferenceUrl(schema.GetPseudo, pseudoClassFunction.Colon.Text + pseudoClassFunction.Function.FunctionName.Text + ")", "http://realworldvalidator.com/css/pseudoclasses/");

            PseudoElementFunctionSelector pseudoElementFunction = item.FindType<PseudoElementFunctionSelector>();
            if (pseudoElementFunction != null)
                return OpenReferenceUrl(schema.GetPseudo, pseudoElementFunction.DoubleColon.Text + pseudoElementFunction.Function.FunctionName.Text + ")", "http://realworldvalidator.com/css/pseudoelements/");

            PseudoElementSelector pseudoElement = item.FindType<PseudoElementSelector>();
            if (pseudoElement != null && pseudoElement.PseudoElement != null)
                return OpenReferenceUrl(schema.GetPseudo, pseudoElement.DoubleColon.Text + pseudoElement.PseudoElement.Text, "http://realworldvalidator.com/css/pseudoelements/");

            PseudoClassSelector pseudoClass = item.FindType<PseudoClassSelector>();
            if (pseudoClass != null && pseudoClass.PseudoClass != null)
                return OpenReferenceUrl(schema.GetPseudo, pseudoClass.Colon.Text + pseudoClass.PseudoClass.Text, "http://realworldvalidator.com/css/pseudoclasses/");

            AtDirective directive = item.FindType<AtDirective>();
            if (directive != null)
                return OpenReferenceUrl(schema.GetAtDirective, directive.At.Text + directive.Keyword.Text, "http://realworldvalidator.com/css/atdirectives/");

            return false;
        }

        private static bool OpenReferenceUrl(Reference reference, string name, string baseUrl)
        {
            ICssCompletionListEntry entry = reference.Invoke(name);
            if (entry != null)
            {
                string text = entry.DisplayText;
                Uri url;

                if (Uri.TryCreate(baseUrl + text, UriKind.Absolute, out url))
                {
                    System.Diagnostics.Process.Start(url.ToString());
                    return true;
                }
            }

            return false;
        }

        protected override bool IsEnabled()
        {
            return TextView.GetSelection("css").HasValue;
        }
    }
}