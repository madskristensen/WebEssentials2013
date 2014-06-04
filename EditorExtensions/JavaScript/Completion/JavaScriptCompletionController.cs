using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.JSLS;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Intel = Microsoft.VisualStudio.Language.Intellisense;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType("javascript")]
    [ContentType("node.js")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class JsTextViewCreationListener : IWpfTextViewConnectionListener
    {
        [Import]
        ICompletionBroker CompletionBroker = null;

        [Import]
        IStandardClassificationService _standardClassifications = null;

        [Import]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public async void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (textView.Properties.ContainsProperty("JsCommandFilter"))
                return;

            if (!subjectBuffers.Any(b => b.ContentType.IsOfType("JavaScript")))
                return;

            var adapter = EditorAdaptersFactoryService.GetViewAdapter(textView);
            var filter = textView.Properties.GetOrCreateSingletonProperty<JsCommandFilter>("JsCommandFilter", () => new JsCommandFilter(textView, CompletionBroker, _standardClassifications));

            int tries = 0;

            // Ugly ugly hack
            // Keep trying to register our filter until after the JSLS CommandFilter
            // is added so we can catch completion before JSLS swallows all of them.
            // To confirm this, click Debug, New Breakpoint, Break at Function, type
            // Microsoft.VisualStudio.JSLS.TextView.TextView.CreateCommandFilter,
            // then make sure that our last filter is added after that runs.
            while (true)
            {
                IOleCommandTarget next;
                adapter.AddCommandFilter(filter, out next);
                filter.Next = next;

                if (IsJSLSInstalled(next) || ++tries > 10)
                    return;
                await Task.Delay(500);
                adapter.RemoveCommandFilter(filter);    // Remove the too-early filter and try again.
            }
        }

        ///<summary>Attempts to figure out whether the JSLS language service has been installed yet.</summary>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget.QueryStatus(System.Guid@,System.UInt32,Microsoft.VisualStudio.OLE.Interop.OLECMD[],System.IntPtr)")]
        static bool IsJSLSInstalled(IOleCommandTarget next)
        {
            Guid cmdGroup = VSConstants.VSStd2K;
            var cmds = new[] { new OLECMD { cmdID = (uint)VSConstants.VSStd2KCmdID.AUTOCOMPLETE } };

            try
            {
                next.QueryStatus(ref cmdGroup, 1, cmds, IntPtr.Zero);
                return cmds[0].cmdf == 3;
            }
            catch
            {
                return false;
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        { }
    }

    internal sealed class JsCommandFilter : IOleCommandTarget
    {
        private readonly IStandardClassificationService _standardClassifications;
        private ICompletionSession _currentSession;
        static readonly Type jsTaggerType = typeof(JavaScriptLanguageService).Assembly.GetType("Microsoft.VisualStudio.JSLS.Classification.Tagger");

        public IWpfTextView TextView { get; private set; }
        public ICompletionBroker Broker { get; private set; }
        public IOleCommandTarget Next { get; set; }

        private static char GetTypeChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        public JsCommandFilter(IWpfTextView textView, ICompletionBroker CompletionBroker, IStandardClassificationService standardClassifications)
        {
            TextView = textView;
            Broker = CompletionBroker;
            _standardClassifications = standardClassifications;
        }

        IEnumerable<IClassificationType> GetCaretClassifications()
        {
            var buffers = TextView.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType("JavaScript") && TextView.GetSelection("JavaScript").HasValue && TextView.GetSelection("JavaScript").Value.Snapshot.TextBuffer == b);

            if (!buffers.Any())
                return Enumerable.Empty<IClassificationType>();

            var tagger = buffers.First().Properties.GetProperty<ITagger<ClassificationTag>>(jsTaggerType);

            return tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(TextView.Caret.Position.BufferPosition, 0)))
                    .Select(s => s.Tag.ClassificationType);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup != VSConstants.VSStd2K || !IsValidTextBuffer())
                return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            // This filter should only have do anything inside a string literal, or when opening a string literal.
            var classifications = GetCaretClassifications();
            bool isInString = classifications.Contains(_standardClassifications.StringLiteral);
            bool isInComment = classifications.Contains(_standardClassifications.Comment);

            var command = (VSConstants.VSStd2KCmdID)nCmdID;
            if (!isInString && !isInComment)
            {
                if (command != VSConstants.VSStd2KCmdID.TYPECHAR)
                    return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                char ch = GetTypeChar(pvaIn);
                if (ch != '"' && ch != '\'' && ch != '@')
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

                        // If the user typed a closing quote, remove any trailing semicolon to match
                        if (_currentSession != null)
                        {

                            var s = _currentSession.SelectedCompletionSet.SelectionStatus;
                            if (s.IsSelected && s.Completion.InsertionText.EndsWith(ch + ";", StringComparison.Ordinal))
                                s.Completion.InsertionText = s.Completion.InsertionText.TrimEnd(';');
                        }

                        var c = Complete(force: false, dontAdvance: true);
                        // If the completion inserted a quote, don't add another one
                        handled = c != null && c.InsertionText.EndsWith(ch.ToString(), StringComparison.Ordinal);
                    }
                    else if (ch == '/')
                    {
                        var c = Complete(force: false, dontAdvance: true);
                        // If the completion inserted a slash, don't add another one.
                        handled = c != null && c.InsertionText.EndsWith("/", StringComparison.Ordinal);
                        // We will re-open completion after handling the keypress, to
                        // show completions for this folder.
                    }
                    else if (isInComment && ch == '@')
                    {
                        var c = Complete(force: false, dontAdvance: true);
                        handled = c != null;
                        // We will re-open completion after handling the keypress, to
                        // show completions for this folder.
                    }
                    break;
                case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
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
                    if (ch == ':' || ch == ' ')
                        Cancel();
                    else if (ch == '"' || ch == '\'' || ch == '/' || ch == '.' || ch == '@' || (!char.IsPunctuation(ch) && !char.IsControl(ch)))
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
                    else
                    {
                        var p = _currentSession.GetTriggerPoint(TextView.TextBuffer.CurrentSnapshot);
                        if (p != null
                            && (p.Value.Position >= p.Value.Snapshot.Length
                             || p.Value.GetChar() != _currentSession.CompletionSets[0].Completions[0].InsertionText[0])
                            )
                            Cancel();
                    }
                    Filter();
                    break;
            }

            return hresult;
        }

        private bool IsValidTextBuffer()
        {
            if (TextView.TextBuffer.ContentType.IsOfType("javascript"))
                return true;

            var projection = TextView.TextBuffer as IProjectionBuffer;

            if (projection != null)
            {
                var snapshotPoint = TextView.Caret.Position.BufferPosition;

                var buffers = projection.SourceBuffers.Where(s => s.ContentType.IsOfType("htmlx"));

                foreach (ITextBuffer buffer in buffers)
                {
                    SnapshotPoint? point = TextView.BufferGraph.MapDownToBuffer(snapshotPoint, PointTrackingMode.Negative, buffer, PositionAffinity.Predecessor);

                    if (point.HasValue)
                        return false;
                }
            }

            return true;
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

        Intel.Completion Complete(bool force, bool dontAdvance = false)
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
                var positionNullable = _currentSession.GetTriggerPoint(TextView.TextBuffer.CurrentSnapshot);
                var completion = _currentSession.SelectedCompletionSet.SelectionStatus.Completion;

                // After this line, _currentSession will be null.  Do not use it.
                _currentSession.Commit();

                if (positionNullable == null)
                    return null;
                var position = positionNullable.Value;

                if (position.Position == TextView.TextBuffer.CurrentSnapshot.Length)
                    return completion;  // If the cursor is at the end of the document, don't choke

                // If applicable, move the cursor to the end of the function call.
                // Unless the user is in completing a deeper Node.js require path,
                // in which case we should stay inside the string.
                if (dontAdvance || completion.InsertionText.EndsWith("/", StringComparison.Ordinal))
                    return completion;

                // If the user completed a Node require path (which won't have any
                // quotes in the completion, move past any existing closing quote.
                // Other completions will include the closing quote themselves, so
                // we don't need to move 
                if (!completion.InsertionText.EndsWith("'", StringComparison.Ordinal) && !completion.InsertionText.EndsWith("\"", StringComparison.Ordinal)
                    && (position.GetChar() == '"' || position.GetChar() == '\''))
                    TextView.Caret.MoveToNextCaretPosition();
                // In either case, if there is a closing parenthesis, move past it
                var prevChar = position.GetChar();
                if ((prevChar == '"' || prevChar == '\'')
                 && TextView.Caret.Position.BufferPosition < TextView.TextBuffer.CurrentSnapshot.Length
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
                    case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}