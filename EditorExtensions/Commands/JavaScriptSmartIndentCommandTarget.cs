using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JavaScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class JavaScriptSmartIndentTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            textView.Properties.GetOrCreateSingletonProperty<JavaScriptSmartIndent>(() => new JavaScriptSmartIndent(textViewAdapter, textView, CompletionBroker));
        }
    }

    class JavaScriptSmartIndent : IOleCommandTarget
    {
        private ITextView _textView;
        private IOleCommandTarget _nextCommandTarget;
        private ICompletionBroker _broker;

        public JavaScriptSmartIndent(IVsTextView adapter, ITextView textView, ICompletionBroker broker)
        {
            _textView = textView;
            _broker = broker;
            adapter.AddCommandFilter(this, out _nextCommandTarget);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                if (nCmdID == 3 && !_broker.IsCompletionActive(_textView))
                {
                    if (Indent())
                    {
                        return VSConstants.S_OK;
                    }
                }

            }

            return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private bool Indent()
        {
            int position = _textView.Caret.Position.BufferPosition.Position;


            if (position == 0 || position == _textView.TextBuffer.CurrentSnapshot.Length || _textView.Selection.SelectedSpans[0].Length > 0)
                return false;

            char before = _textView.TextBuffer.CurrentSnapshot.GetText(position - 1, 1)[0];
            char after = _textView.TextBuffer.CurrentSnapshot.GetText(position, 1)[0];

            if (before == '{' && after == '}')
            {
                EditorExtensionsPackage.DTE.UndoContext.Open("Smart indent");

                _textView.TextBuffer.Insert(position, Environment.NewLine + '\t');
                SnapshotPoint point = new SnapshotPoint(_textView.TextBuffer.CurrentSnapshot, position);
                _textView.Selection.Select(new SnapshotSpan(point, 4), true);

                EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");

                _textView.Selection.Clear();
                SendKeys.Send("{ENTER}");

                EditorExtensionsPackage.DTE.UndoContext.Close();

                return true;
            }

            return false;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch (prgCmds[i].cmdID)
                    {
                        case 3:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            }

            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}