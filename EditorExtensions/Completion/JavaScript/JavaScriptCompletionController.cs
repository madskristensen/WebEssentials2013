﻿using Microsoft.VisualStudio;
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("javascript")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class JsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        IVsEditorAdaptersFactoryService AdaptersFactory = null;

        [Import]
        ICompletionBroker CompletionBroker = null;

        [Import]
        IStandardClassificationService _standardClassifications;

        public async void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            int tries = 0;

            // Ugly ugly hack
            // Keep trying to register our filter until after the JSLS CommandFilter
            // is added so we can catch completion before JSLS swallows all of them.
            // To confirm this, click Debug, New Breakpoint, Break at Function, type
            // Microsoft.VisualStudio.JSLS.TextView.TextView.CreateCommandFilter,
            // then make sure that our last filter is added after that runs.
            JsCommandFilter filter = new JsCommandFilter(view, CompletionBroker, _standardClassifications);
            while (true)
            {
                IOleCommandTarget next;
                textViewAdapter.AddCommandFilter(filter, out next);
                filter.Next = next;

                if (IsJSLSInstalled(next) || ++tries > 10)
                    return;
                await Task.Delay(500);
                textViewAdapter.RemoveCommandFilter(filter);    // Remove the too-early filter and try again.
            }
        }

        ///<summary>Attempts to figure out whether the JSLS language service has been installed yet.</summary>
        static bool IsJSLSInstalled(IOleCommandTarget next)
        {
            Guid cmdGroup = VSConstants.VSStd2K;
            var cmds = new[] { new OLECMD { cmdID = (uint)VSConstants.VSStd2KCmdID.AUTOCOMPLETE } };
            next.QueryStatus(ref cmdGroup, 1, cmds, IntPtr.Zero);
            return cmds[0].cmdf == 3;
        }
    }

    internal sealed class JsCommandFilter : IOleCommandTarget
    {
        private readonly IStandardClassificationService _standardClassifications;
        private ICompletionSession _currentSession;

        public JsCommandFilter(IWpfTextView textView, ICompletionBroker broker, IStandardClassificationService standardClassifications)
        {
            _currentSession = null;

            TextView = textView;
            Broker = broker;
            _standardClassifications = standardClassifications;
        }

        public IWpfTextView TextView { get; private set; }
        public ICompletionBroker Broker { get; private set; }
        public IOleCommandTarget Next { get; set; }

        private char GetTypeChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        static readonly Type jsTaggerType = typeof(Microsoft.VisualStudio.JSLS.JavaScriptLanguageService).Assembly.GetType("Microsoft.VisualStudio.JSLS.Classification.Tagger");

        IEnumerable<IClassificationType> GetCaretClassifications()
        {
            var tagger = TextView.TextBuffer.Properties.GetProperty<ITagger<ClassificationTag>>(jsTaggerType);
            return tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(TextView.Caret.Position.BufferPosition, 0)))
                    .Select(s => s.Tag.ClassificationType);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup != VSConstants.VSStd2K)
                return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            // This filter should only have do anything inside a string literal, or when opening a string literal.
            bool isInString = GetCaretClassifications().Contains(_standardClassifications.StringLiteral);

            var command = (VSConstants.VSStd2KCmdID)nCmdID;
            if (!isInString)
            {
                if (command != VSConstants.VSStd2KCmdID.TYPECHAR)
                    return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                char ch = GetTypeChar(pvaIn);
                if (ch != '"' && ch != '\'')
                    return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            bool closedCompletion = false;
            bool handled = false;

            // 1. Pre-process
            switch (command)
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
                    // Never handle this command; we always want JSLS to try too.
                    StartSession();
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

            int hresult = VSConstants.S_OK;
            if (!handled)
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (!ErrorHandler.Succeeded(hresult))
                return hresult;

            switch (command)
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

                if (TextView.Caret.Position.BufferPosition.Position == TextView.TextBuffer.CurrentSnapshot.Length)
                    return completion;  // If the cursor is at the end of the document, don't choke

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
}