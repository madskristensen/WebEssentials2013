using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace MadsKristensen.EditorExtensions
{
    internal class F1Help : CommandTargetBase
    {
        private CssTree _tree;

        public F1Help(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(VSConstants.VSStd97CmdID).GUID, (uint)VSConstants.VSStd97CmdID.F1Help)
        { }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!EnsureInitialized())
                return false;

            int position = TextView.Caret.Position.BufferPosition.Position;
            ParseItem item = _tree.StyleSheet.ItemBeforePosition(position);

            if (item == null)
                return false;

            return SchemaLookup(item);
        }

        private delegate ICssCompletionListEntry Reference(string name);

        private bool SchemaLookup(ParseItem item)
        {
            if (item is ClassSelector || item is IdSelector || item is ItemName || item.Parent is RuleBlock || item.Parent is StyleSheet)
                return false;

            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaRootForBuffer(TextView.TextBuffer);

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

        private bool OpenReferenceUrl(Reference reference, string name, string baseUrl)
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

        public bool EnsureInitialized()
        {
            if (_tree == null && Microsoft.Web.Editor.WebEditor.Host != null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(TextView.TextBuffer);
                    _tree = document.Tree;
                }
                catch (ArgumentNullException)
                { }
            }

            return _tree != null;
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}