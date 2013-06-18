using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Windows.Forms;
using System.Windows.Input;

namespace MadsKristensen.EditorExtensions
{
    internal class SpeedTypingTarget : IOleCommandTarget
    {
        private IWpfTextView _textView;
        private ICompletionBroker _broker;
        private IQuickInfoBroker _QuickInfobroker;
        private IClassifierAggregatorService _aggregator;
        private DTE2 _dte;
        private CssTree _tree;

        public SpeedTypingTarget(CssSortPropertiesViewCreationListener componentContext, IVsTextView adapter, IWpfTextView textView)
        {
            this._dte = EditorExtensionsPackage.DTE;
            this._textView = textView;
            this._aggregator = componentContext.AggregatorService;
            this._broker = componentContext.CompletionBroker;
            this._QuickInfobroker = componentContext.QuickInfoBroker;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            //foreach (OutputWindowPane item in WebEssentialsPackage.dte.ToolWindows.OutputWindow.OutputWindowPanes)
            //{
            //    item.OutputString(nCmdID.ToString() + Environment.NewLine);
            //}

            if (pguidCmdGroup == VSConstants.VSStd2K && WESettings.GetBoolean(WESettings.Keys.EnableSpeedTyping))
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.RETURN:
                        if (Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftShift))
                        {
                            if (Jump()) return
                                VSConstants.S_OK;
                        }
                        else
                        {
                            CommitStatementCompletion();
                            if (Process(true, true, true) == VSConstants.S_OK) return VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.TAB:
                        var completion = CommitStatementCompletion();
                        if (Process(false, true, false) == VSConstants.S_OK || completion) return VSConstants.S_OK;
                        break;
                }
            }

            return (int)(Constants.MSOCMDERR_E_FIRST);
        }

        private bool Jump()
        {
            if (!EnsureInitialized())
                return false;

            int position = _textView.Caret.Position.BufferPosition.Position;
            ParseItem item = _tree.StyleSheet.ItemBeforePosition(position);

            if (item != null)
            {
                RuleBlock rule = item.FindType<RuleBlock>();
                Declaration dec = item.FindType<Declaration>();

                if (rule != null && dec != null)
                {
                    CommitStatementCompletion();

                    var line = _textView.TextSnapshot.GetLineFromPosition(position);
                    string text = line.Extent.GetText().TrimEnd();

                    if (!string.IsNullOrWhiteSpace(text) && !text.EndsWith(";"))
                    {
                        using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
                        {
                            edit.Replace(line.Extent, text + ";");
                            edit.Apply();
                        }
                    }

                    EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");

                    SnapshotPoint point = new SnapshotPoint(_textView.TextBuffer.CurrentSnapshot, rule.AfterEnd);
                    _textView.Caret.MoveTo(point);
                    _textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(point, 0));
                    return true;
                }
            }

            return false;
        }

        //private int JumpOut()
        //{
        //    int result = VSConstants.S_FALSE;
        //    var span = _textView.Selection.SelectedSpans[0];
        //    var position = span.Start.Position;
        //    var line = span.Start.GetContainingLine();
        //    var classifications = _aggregator.GetClassifier(_textView.TextBuffer).GetClassificationSpans(line.Extent);

        //    _dte.UndoContext.Open("Jump out of brace");
        //    try
        //    {
        //        foreach (var classification in classifications)
        //        {
        //            if (IsPropertyValue(classification) && IsPropertyValueEligible(line, position))
        //            {
        //                CommitStatementCompletion();
        //                line = _textView.TextSnapshot.GetLineFromPosition(position);
        //                using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
        //                {
        //                    edit.Replace(line.Extent, line.Extent.GetText().TrimEnd() + ";");
        //                    edit.Apply();
        //                }
        //            }
        //            else if (IsSelector(classification) || (IsPropertyName(classification) && !line.Extent.GetText().Contains(":")))
        //            {
        //                return VSConstants.S_FALSE;
        //            }
        //        }

        //        var text = _textView.TextSnapshot.GetText();
        //        int start = text.LastIndexOf('{', position - 1);
        //        int middle = text.IndexOf('{', position - 1);
        //        int end = text.IndexOf('}', position - 1);
        //        int emptyLines = ResolveEmptyLines(end);

        //        string blanks = string.Empty;
        //        if (emptyLines < 3)
        //        {
        //            for (int i = 0; i < (3 - emptyLines); i++)
        //            {
        //                blanks += "\n";
        //            }
        //        }

        //        if ((end < middle || middle == -1) && start < position && end > position)
        //        {
        //            using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
        //            {
        //                edit.Replace(_textView.TextSnapshot.GetLineFromPosition(end).Extent, "}" + blanks);

        //                if (string.IsNullOrWhiteSpace(line.GetText()))
        //                {
        //                    edit.Delete(line.ExtentIncludingLineBreak);
        //                    end = end - line.ExtentIncludingLineBreak.Length;
        //                }

        //                edit.Apply();
        //                result = VSConstants.S_OK;
        //            }

        //            _textView.Caret.MoveTo(new SnapshotPoint(_textView.TextSnapshot, end + 3));
        //            _broker.DismissAllSessions(_textView);
        //            DismissQuickInfo();
        //        }
        //    }
        //    finally
        //    {
        //        _dte.UndoContext.Close();
        //    }

        //    return result;
        //}

        //private int ResolveEmptyLines(int end)
        //{
        //    if (end == -1 || _textView.TextSnapshot.GetLineNumberFromPosition(end) == _textView.TextSnapshot.LineCount)
        //        return 0;

        //    int emptyLines = 0;
        //    int currentLine = _textView.TextSnapshot.GetLineFromPosition(end).LineNumber + 1;
        //    while ((currentLine + emptyLines) < _textView.TextSnapshot.LineCount)
        //    {
        //        if (string.IsNullOrWhiteSpace(_textView.TextSnapshot.GetLineFromLineNumber(currentLine + emptyLines).GetText()))
        //        {
        //            emptyLines++;
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        //    return emptyLines;
        //}

        private int Process(bool selector, bool name, bool value)
        {
            if (!EnsureInitialized())
                return VSConstants.S_FALSE;

            var span = _textView.Selection.SelectedSpans[0];
            var line = span.Start.GetContainingLine();
            var position = span.Start.Position;// -(line.Length - line.GetText().TrimEnd().Length);
            var classifications = _aggregator.GetClassifier(_textView.TextBuffer).GetClassificationSpans(line.Extent);

            foreach (var classification in classifications)
            {
                if (selector && IsSelector(classification) && IsSelectorEligible(line, position))
                {
                    return InsertBraces(line);
                }
                else if (name && IsPropertyName(classification) && IsPropertyNameEligible(line))
                {
                    DismissQuickInfo();
                    return InsertColon(position);
                }
                else if (value && IsPropertyValue(classification) && IsPropertyValueEligible(line, position))
                {
                    DismissQuickInfo();
                    return InsertSemiColon(line);
                }
            }

            return VSConstants.S_FALSE;
        }

        private static bool IsSelector(ClassificationSpan classification)
        {
            return classification.ClassificationType.Classification == "CSS Selector";
        }

        private static bool IsPropertyName(ClassificationSpan classification)
        {
            return classification.ClassificationType.Classification == "CSS Property Name";
        }

        private static bool IsPropertyValue(ClassificationSpan classification)
        {
            return classification.ClassificationType.Classification == "CSS Property Value";
        }

        private bool IsSelectorEligible(ITextSnapshotLine line, int position)
        {
            string text = line.GetText();
            if (text.IndexOf('{') > -1)
                return false;

            if (text.Trim().EndsWith(",", StringComparison.Ordinal))
                return false;

            if (line.LineNumber + 1 < line.Snapshot.LineCount)
            {
                var next = line.Snapshot.GetLineFromLineNumber(line.LineNumber + 1);
                if (next.GetText().Trim().StartsWith("{", StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private bool IsPropertyValueEligible(ITextSnapshotLine line, int position)
        {
            string text = line.GetText();
            int diff = text.Length - text.TrimEnd().Length;

            if (line.End.Position - diff > position)
                return false;

            if (text.IndexOf(';') > -1)
                return false;

            return true;
        }

        private bool IsPropertyNameEligible(ITextSnapshotLine line)
        {
            return !line.GetText().Contains(":");
        }

        private bool CommitStatementCompletion()
        {
            bool value = _broker.IsCompletionActive(_textView);

            if (_broker.IsCompletionActive(_textView))
            {
                _broker.GetSessions(_textView)[0].Commit();
            }

            return value;
        }

        private void DismissQuickInfo()
        {
            if (_QuickInfobroker.IsQuickInfoActive(_textView))
                _QuickInfobroker.GetSessions(_textView)[0].Dismiss();
        }

        private int InsertBraces(ITextSnapshotLine line)
        {
            string text = line.GetText();

            using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
            {
                _dte.UndoContext.Open("Insert braces");
                edit.Replace(line.Extent, text.TrimEnd() + " {\n\t\n}");
                edit.Apply();
                _dte.UndoContext.Close();
            }

            SendKeys.Send("{LEFT}{LEFT}^( )");
            return VSConstants.S_OK;
        }

        private int InsertColon(int position)
        {
            using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
            {
                _dte.UndoContext.Open("Insert braces");
                edit.Insert(position, ":");
                edit.Apply();
                _dte.UndoContext.Close();
            }

            SendKeys.Send(" ");

            return VSConstants.S_OK;
        }

        private int InsertSemiColon(ITextSnapshotLine line)
        {
            string text = line.GetText();

            using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
            {
                _dte.UndoContext.Open("Insert braces");
                edit.Replace(line.Extent, text.TrimEnd() + ";\n\t");
                edit.Apply();
                _dte.UndoContext.Close();
            }

            //SendKeys.Send("^( )");
            return VSConstants.S_OK;
        }

        public bool EnsureInitialized()
        {
            if (_tree == null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_textView.TextBuffer);
                    _tree = document.Tree;
                }
                catch (ArgumentNullException)
                {
                }
            }

            return _tree != null;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            //if (WESettings.GetBoolean(WESettings.Keys.EnableSpeedTyping))
            //{
            //    for (int i = 0; i < cCmds; i++)
            //    {
            //        switch ((VSConstants.VSStd2KCmdID)prgCmds[i].cmdID)
            //        {
            //            case VSConstants.VSStd2KCmdID.RETURN:
            //                prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
            //                return VSConstants.S_OK;
            //        }
            //    }
            //}

            //return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            return (int)(Constants.OLECMDERR_E_NOTSUPPORTED);
        }
    }
}
