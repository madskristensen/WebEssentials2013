using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace WebEssentialsTests
{
    internal class MockSnapshot : ITextSnapshot
    {
        private readonly string text;

        public int Length { get { return text.Length; } }
        public char this[int position] { get { return text[position]; } }

        public MockSnapshot(string text)
        {
            this.text = text;
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            text.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public void Write(System.IO.TextWriter writer)
        {
            writer.Write(text);
        }

        public void Write(System.IO.TextWriter writer, Span span)
        {
            writer.Write(GetText(span));
        }

        public string GetText()
        {
            return text;
        }

        public string GetText(int startIndex, int length)
        {
            return text.Substring(startIndex, length);
        }

        public string GetText(Span span)
        {
            return GetText(span.Start, span.Length);
        }

        public char[] ToCharArray(int startIndex, int length)
        {
            return text.ToCharArray(startIndex, length);
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        { throw new NotImplementedException(); }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        { throw new NotImplementedException(); }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        { throw new NotImplementedException(); }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        { throw new NotImplementedException(); }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        { throw new NotImplementedException(); }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        { throw new NotImplementedException(); }

        public ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        { throw new NotImplementedException(); }

        public ITextSnapshotLine GetLineFromPosition(int position)
        { throw new NotImplementedException(); }

        public int GetLineNumberFromPosition(int position)
        { throw new NotImplementedException(); }

        public Microsoft.VisualStudio.Utilities.IContentType ContentType { get { throw new NotImplementedException(); } }
        public int LineCount { get { throw new NotImplementedException(); } }
        public IEnumerable<ITextSnapshotLine> Lines { get { throw new NotImplementedException(); } }
        public ITextBuffer TextBuffer { get { throw new NotImplementedException(); } }
        public ITextVersion Version { get { throw new NotImplementedException(); } }
    }
}
