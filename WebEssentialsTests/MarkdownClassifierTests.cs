using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.VisualStudio.Text.Classification;
using System.Collections.Generic;
using Microsoft.Html.Core;
using Microsoft.VisualStudio.Text;

namespace WebEssentialsTests
{
    [TestClass]
    public class MarkdownClassifierTests
    {
        static List<Tuple<string, string>> Classify(string markdown, Span? subSpan = null)
        {
            var artifacts = new ArtifactCollection(new MarkdownCodeArtifactProcessor());
            artifacts.Build(markdown);
            var classifier = new MarkdownClassifier(artifacts, new MockClassificationTypeRegistry());

            return classifier.GetClassificationSpans(new SnapshotSpan(new MockSnapshot(markdown), subSpan ?? new Span(0, markdown.Length)))
                             .Select(cs => Tuple.Create(cs.ClassificationType.Classification, markdown.Substring(cs.Span.Start, cs.Span.Length)))
                             .ToList();
        }

        static Func<string, Tuple<string, string>> CreateCreator(string classificationType) { return source => Tuple.Create(classificationType, source); }

        static readonly Func<string, Tuple<string, string>> Bold = CreateCreator(MarkdownClassificationTypes.MarkdownBold);
        static readonly Func<string, Tuple<string, string>> Code = CreateCreator(MarkdownClassificationTypes.MarkdownCode);
        static readonly Func<string, Tuple<string, string>> Header = CreateCreator(MarkdownClassificationTypes.MarkdownHeader);
        static readonly Func<string, Tuple<string, string>> Italic = CreateCreator(MarkdownClassificationTypes.MarkdownItalic);
        static readonly Func<string, Tuple<string, string>> Quote = CreateCreator(MarkdownClassificationTypes.MarkdownQuote);

        [TestMethod]
        public void TestBasicClassifications()
        {
            CollectionAssert.AreEqual(
                new[]{
                    Bold("**bold**"),
                    Italic("_italic_"),
                    Header("#Header"),
                    Header("##Header2"),
                    Quote(" ##Header2"),
                },
                Classify(@"**bold**, _italic_
#Header
>> ##Header2
"));
        }
    }

    class MockClassificationTypeRegistry : IClassificationTypeRegistryService
    {
        public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes)
        { throw new NotImplementedException(); }
        public IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes)
        { throw new NotImplementedException(); }
        public IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes)
        { throw new NotImplementedException(); }

        public IClassificationType GetClassificationType(string type)
        {
            return new MockType(type);
        }
        class MockType : IClassificationType
        {
            public MockType(string type) { Classification = type; }

            public IEnumerable<IClassificationType> BaseTypes { get { yield break; } }
            public string Classification { get; private set; }

            public bool IsOfType(string type)
            {
                return Classification.Equals(type, StringComparison.OrdinalIgnoreCase) || BaseTypes.Any(b => b.IsOfType(type));
            }
        }
    }
    class MockSnapshot : ITextSnapshot
    {
        readonly string text;

        public MockSnapshot(string text)
        {
            this.text = text;
        }
        public int Length { get { return text.Length; } }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) { text.CopyTo(sourceIndex, destination, destinationIndex, count); }

        public void Write(System.IO.TextWriter writer) { writer.Write(text); }
        public void Write(System.IO.TextWriter writer, Span span) { writer.Write(GetText(span)); }

        public string GetText() { return text; }
        public string GetText(int startIndex, int length) { return text.Substring(startIndex, length); }
        public string GetText(Span span) { return GetText(span.Start, span.Length); }

        public char[] ToCharArray(int startIndex, int length) { return text.ToCharArray(startIndex, length); }
        public char this[int position] { get { return text[position]; } }

        public Microsoft.VisualStudio.Utilities.IContentType ContentType { get { throw new NotImplementedException(); } }

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
        public int LineCount { get { throw new NotImplementedException(); } }
        public IEnumerable<ITextSnapshotLine> Lines { get { throw new NotImplementedException(); } }
        public ITextBuffer TextBuffer { get { throw new NotImplementedException(); } }
        public ITextVersion Version { get { throw new NotImplementedException(); } }
    }
}
