﻿using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using ZenCoding;

namespace MadsKristensen.EditorExtensions
{
    internal class ZenCoding : CommandTargetBase
    {
        private ICompletionBroker _broker;
        private ITrackingSpan _trackingSpan;

        private static Regex _bracket = new Regex(@"<([a-z0-9]*)\b[^>]*>([^<]*)</\1>", RegexOptions.IgnoreCase);
        private static Regex _quotes = new Regex("(=\"()\")", RegexOptions.IgnoreCase);

        public ZenCoding(IVsTextView adapter, IWpfTextView textView, ICompletionBroker broker)
            : base(adapter, textView, typeof(VSConstants.VSStd2KCmdID).GUID, 4, 5)
        {
            _broker = broker;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (commandId == 4 && !_broker.IsCompletionActive(TextView))
            {
                if (WESettings.GetBoolean(WESettings.Keys.EnableHtmlZenCoding))
                {
                    if (InvokeZenCoding())
                    {
                        return true;
                    }
                    else if (MoveToNextEmptySlot())
                    {
                        return true;
                    }
                }
            }
            else if (commandId == 5 && !_broker.IsCompletionActive(TextView))
            {
                if (WESettings.GetBoolean(WESettings.Keys.EnableHtmlZenCoding) && MoveToPrevEmptySlot())
                {
                    return true;
                }
            }

            return false;
        }

        private bool MoveToNextEmptySlot()
        {
            if (_trackingSpan == null)
                return false;

            int position = TextView.Caret.Position.BufferPosition.Position + 1;
            Span ts = _trackingSpan.GetSpan(TextView.TextBuffer.CurrentSnapshot);

            if (ts.Contains(position))
            {
                Span span = new Span(position, ts.End - position);
                SetCaret(span, false);
                return true;
            }

            return false;
        }

        private bool MoveToPrevEmptySlot()
        {
            if (_trackingSpan == null)
                return false;

            int position = TextView.Caret.Position.BufferPosition.Position;

            if (position > 0)
            {
                Span ts = _trackingSpan.GetSpan(TextView.TextBuffer.CurrentSnapshot);

                if (ts.Contains(position - 1))
                {
                    Span span = new Span(ts.Start, position - ts.Start - 1);
                    SetCaret(span, true);
                    return true;
                }
            }

            return false;
        }

        private bool InvokeZenCoding()
        {
            Span zenSpan = GetText();

            if (zenSpan.Length == 0 || TextView.Selection.SelectedSpans[0].Length > 0 || !IsValidTextBuffer())
                return false;

            string zenSyntax = TextView.TextBuffer.CurrentSnapshot.GetText(zenSpan);

            Parser parser = new Parser();
            string result = parser.Parse(zenSyntax, ZenType.HTML);

            if (!string.IsNullOrEmpty(result))
            {
                EditorExtensionsPackage.DTE.UndoContext.Open("ZenCoding");

                ITextSelection selection = UpdateTextBuffer(zenSpan, result);
                Span newSpan = new Span(zenSpan.Start, selection.SelectedSpans[0].Length);

                if (result.Count(c => c == '>') > 2)
                {
                    _trackingSpan = TextView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(newSpan, SpanTrackingMode.EdgeExclusive);
                }

                selection.Clear();

                EditorExtensionsPackage.DTE.UndoContext.Close();

                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => SetCaret(newSpan, false)), DispatcherPriority.Normal, null);

                return true;
            }

            return false;
        }

        private bool IsValidTextBuffer()
        {
            var projection = TextView.TextBuffer as IProjectionBuffer;

            if (projection != null)
            {
                var snapshotPoint = TextView.Caret.Position.BufferPosition;

                var buffers = projection.SourceBuffers.Where(
                    s =>
                        !s.ContentType.IsOfType("html")
                        && !s.ContentType.IsOfType("htmlx")
                        && !s.ContentType.IsOfType("inert")
                        && !s.ContentType.IsOfType("CSharp")
                        && !s.ContentType.IsOfType("VisualBasic")
                        && !s.ContentType.IsOfType("RoslynCSharp")
                        && !s.ContentType.IsOfType("RoslynVisualBasic"));


                foreach (ITextBuffer buffer in buffers)
                {
                    SnapshotPoint? point = TextView.BufferGraph.MapDownToBuffer(snapshotPoint, PointTrackingMode.Negative, buffer, PositionAffinity.Predecessor);

                    if (point.HasValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        //private bool IsValidTextBuffer()
        //{
        //    IProjectionBuffer projection = _textView.TextBuffer as IProjectionBuffer;            

        //    if (projection != null)
        //    {
        //        int position = _textView.Caret.Position.BufferPosition.Position;
        //        var buffers = projection.SourceBuffers.Where(s => s.ContentType.IsOfType("css") || s.ContentType.IsOfType("javascript"));

        //        foreach (ITextBuffer buffer in buffers)
        //        {
        //            IProjectionSnapshot snapshot = buffer.CurrentSnapshot as IProjectionSnapshot;
        //            bool containsPosition = snapshot.GetSourceSpans().Any(s => s.Contains(position));

        //            if (containsPosition)
        //            {
        //                return false;
        //            }
        //        }
        //    }

        //    return true;
        //}

        private bool SetCaret(Span zenSpan, bool isReverse)
        {
            string text = TextView.TextBuffer.CurrentSnapshot.GetText();
            Span quote = FindTabSpan(zenSpan, isReverse, text, _quotes);
            Span bracket = FindTabSpan(zenSpan, isReverse, text, _bracket);

            if (!isReverse && bracket.Start > 0 && (bracket.Start < quote.Start || quote.Start == 0))
            {
                quote = bracket;
            }
            else if (isReverse && bracket.Start > 0 && (bracket.Start > quote.Start || quote.Start == 0))
            {
                quote = bracket;
            }

            if (zenSpan.Contains(quote.Start))
            {
                MoveTab(quote);
                return true;
            }
            else if (!isReverse)
            {
                MoveTab(new Span(zenSpan.End, 0));
                return true;
            }

            return false;
        }

        private void MoveTab(Span quote)
        {
            TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, quote.Start));
            SnapshotSpan span = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, quote);
            TextView.Selection.Select(span, false);
        }

        private static Span FindTabSpan(Span zenSpan, bool isReverse, string text, Regex regex)
        {
            MatchCollection matches = regex.Matches(text);

            if (!isReverse)
            {
                foreach (Match match in matches)
                {
                    Group group = match.Groups[2];

                    if (group.Index >= zenSpan.Start)
                    {
                        return new Span(group.Index, group.Length);
                    }
                }
            }
            else
            {
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    Group group = matches[i].Groups[2];

                    if (group.Index < zenSpan.End)
                    {
                        return new Span(group.Index, group.Length);
                    }
                }
            }

            return new Span();
        }

        private ITextSelection UpdateTextBuffer(Span zenSpan, string result)
        {
            TextView.TextBuffer.Replace(zenSpan, result);

            SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, zenSpan.Start);
            SnapshotSpan snapshot = new SnapshotSpan(point, result.Length);
            TextView.Selection.Select(snapshot, false);

            EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");

            return TextView.Selection;
        }

        private Span GetText()
        {
            int position = TextView.Caret.Position.BufferPosition.Position;

            if (position >= 0)
            {
                var line = TextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(position);
                string text = line.GetText().TrimEnd();

                if (string.IsNullOrWhiteSpace(text) || text.Length < position - line.Start || text.Length + line.Start > position)
                    return new Span();

                string result = text.Substring(0, position - line.Start).TrimStart();

                if (result.Length > 0 && !text.Contains("<") && !char.IsWhiteSpace(result.Last()))
                {
                    return new Span(line.Start.Position + text.IndexOf(result), result.Length);
                }
            }

            return new Span();
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}