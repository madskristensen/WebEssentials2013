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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MadsKristensen.EditorExtensions
{
    #region Command Filter

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("javascript")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class JsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        IVsEditorAdaptersFactoryService AdaptersFactory = null;

        [Import]
        ICompletionBroker CompletionBroker = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            JsCommandFilter filter = new JsCommandFilter(view, CompletionBroker);

            IOleCommandTarget next;
            textViewAdapter.AddCommandFilter(filter, out next);
            filter.Next = next;
        }
    }

    internal sealed class JsCommandFilter : IOleCommandTarget
    {
        private ICompletionSession _currentSession;

        public JsCommandFilter(IWpfTextView textView, ICompletionBroker broker)
        {
            _currentSession = null;

            TextView = textView;
            Broker = broker;
        }

        public IWpfTextView TextView { get; private set; }
        public ICompletionBroker Broker { get; private set; }
        public IOleCommandTarget Next { get; set; }

        private char GetTypeChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            bool closedCompletion = false;
            bool handled = false;
            int hresult = VSConstants.S_OK;

            // 1. Pre-process
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                        char ch = GetTypeChar(pvaIn);
                        if (ch == '"' || ch == '\'' && _currentSession != null)
                        {
                            // If the user commits a completion from a closing quote, do
                            // not immediately re-open the completion window below.
                            closedCompletion = _currentSession != null;
                            var c = Complete(force: false, dontAdvance: true);
                            // If the completion inserted a quote, don't add another one
                            handled = c != null && c.InsertionText.EndsWith(ch.ToString());
                        }
                        else if (ch == '/')
                        {
                            var c = Complete(force: false, dontAdvance: true);
                            // If the completion inserted a slash, don't add another one.
                            handled = c != null && c.InsertionText.EndsWith("/");
                            // We will re-open completion after handling the keypress, to
                            // show completions for this folder.
                        }
                        break;
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        handled = StartSession();
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        handled = Complete(false) != null;
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        handled = Complete(true) != null;
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        handled = Cancel();
                        break;
                }
            }

            if (!handled)
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (ErrorHandler.Succeeded(hresult))
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.TYPECHAR:
                            char ch = GetTypeChar(pvaIn);
                            if (ch == ':')
                                Cancel();
                            else if (ch == '"' || ch == '\'' || ch == '/' || ch == '.' || (!char.IsPunctuation(ch) && !char.IsControl(ch)))
                            {
                                if (!closedCompletion)
                                    StartSession();
                            }
                            else if (_currentSession != null)
                                Filter();
                            break;
                        case VSConstants.VSStd2KCmdID.DELETE:
                        case VSConstants.VSStd2KCmdID.DELETEWORDLEFT:
                        case VSConstants.VSStd2KCmdID.DELETEWORDRIGHT:
                        case VSConstants.VSStd2KCmdID.BACKSPACE:
                            if (_currentSession == null)
                                StartSession();

                            Filter();
                            break;
                    }
                }
            }

            return hresult;
        }

        private void Filter()
        {
            if (_currentSession == null)
                return;

            _currentSession.SelectedCompletionSet.SelectBestMatch();
            _currentSession.SelectedCompletionSet.Recalculate();
        }

        bool Cancel()
        {
            if (_currentSession == null)
                return false;

            _currentSession.Dismiss();

            return true;
        }

        Completion Complete(bool force, bool dontAdvance = false)
        {
            if (_currentSession == null)
                return null;

            if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
            {
                _currentSession.Dismiss();
                return null;
            }
            else
            {
                var completion = _currentSession.SelectedCompletionSet.SelectionStatus.Completion;
                _currentSession.Commit();

                // If applicable, move the cursor to the end of the function call.
                // Unless the user is in completing a deeper Node.js require path,
                // in which case we should stay inside the string.
                if (dontAdvance || completion.InsertionText.EndsWith("/"))
                    return completion;

                // If the user completed a Node require path (which won't have any
                // quotes in the completion, move past any existing closing quote.
                // Other completions will include the closing quote themselves, so
                // we don't need to move 
                if (!completion.InsertionText.EndsWith("'") && !completion.InsertionText.EndsWith("\"")
                    && (TextView.Caret.Position.BufferPosition.GetChar() == '"' || TextView.Caret.Position.BufferPosition.GetChar() == '\''))
                    TextView.Caret.MoveToNextCaretPosition();
                // In either case, if there is a closing parenthesis, move past it
                var prevChar = (TextView.Caret.Position.BufferPosition - 1).GetChar();
                if ((prevChar == '"' || prevChar == '\'')
                 && TextView.Caret.Position.BufferPosition.GetChar() == ')')
                    TextView.Caret.MoveToNextCaretPosition();
                return completion;
            }
        }

        bool StartSession()
        {
            if (_currentSession != null)
                return false;

            SnapshotPoint caret = TextView.Caret.Position.BufferPosition;
            ITextSnapshot snapshot = caret.Snapshot;

            if (!Broker.IsCompletionActive(TextView))
            {
                _currentSession = Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
            }
            else
            {
                _currentSession = Broker.GetSessions(TextView)[0];
            }
            _currentSession.Dismissed += (sender, args) => _currentSession = null;

            if (!_currentSession.IsStarted)
                _currentSession.Start();

            return true;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }

    #endregion
}