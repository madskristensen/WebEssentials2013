using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.Web.Editor;
using Intel = Microsoft.VisualStudio.Language.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    class UseDirectiveCompletionSource : StringCompletionSource
    {
        public override Span? GetInvocationSpan(string text, int linePosition, SnapshotPoint position)
        {
            // If this isn't the beginning of the line, stop immediately.
            var quote = text.SkipWhile(Char.IsWhiteSpace).FirstOrDefault();
            if (quote != '"' && quote != '\'')
                return null;

            // If it is, make sure it's also the beginning of a function.
            var prevLine = EnumeratePrecedingLineTokens(position).GetEnumerator();

            // If we are at the beginning of the file, that is also fine.
            if (prevLine.MoveNext())
            {
                // Check that the previous line contains "function", then
                // eventually ") {" followed by the end of the line.
                if (!ConsumeToToken(prevLine, "function", "keyword") || !ConsumeToToken(prevLine, ")", "operator"))
                    return null;
                if (!prevLine.MoveNext() || prevLine.Current.Span.GetText() != "{")
                    return null;
                // If there is non-comment code after the function, stop
                if (prevLine.MoveNext() && SkipComments(prevLine))
                    return null;
            }

            var startIndex = text.TakeWhile(Char.IsWhiteSpace).Count();
            var endIndex = linePosition;

            // Consume the auto-added close quote, if present.
            // If range ends at the end of the line, we cannot
            // check this.
            if (endIndex < text.Length && text[endIndex] == quote)
                endIndex++;
            return Span.FromBounds(startIndex, endIndex);
        }

        static readonly ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupIntrinsic, StandardGlyphItem.GlyphItemPublic);
        public override IEnumerable<Intel.Completion> GetEntries(char quote, SnapshotPoint caret)
        {
            return new[] { "use strict", "use asm" }.Select(s => new Intel.Completion(
                quote + s + quote + ";",
                quote + s + quote + ";",
                "Instructs that this block be processed in " + s.Substring(4) + " mode by supporting JS engines",
                _glyph,
                null)
            );
        }

        static readonly Type jsTaggerType = typeof(Microsoft.VisualStudio.JSLS.JavaScriptLanguageService).Assembly.GetType("Microsoft.VisualStudio.JSLS.Classification.Tagger");

        // Inspired by NodejsTools
        /// <summary>
        /// Enumerates the classifications in the first code line preceding a point.
        /// Skips blank lines or comment-only lines.
        /// </summary>
        private static IEnumerable<ClassificationSpan> EnumeratePrecedingLineTokens(SnapshotPoint start)
        {
            var tagger = start.Snapshot.TextBuffer.Properties.GetProperty<ITagger<ClassificationTag>>(jsTaggerType);

            var curLine = start.GetContainingLine();
            if (curLine.LineNumber == 0)
                yield break;

            bool foundCode = false;
            do
            {
                curLine = start.Snapshot.GetLineFromLineNumber(curLine.LineNumber - 1);
                var classifications = tagger.GetTags(new NormalizedSnapshotSpanCollection(curLine.Extent));
                foreach (var tag in classifications.Where(c => !c.Tag.ClassificationType.IsOfType("comment")))
                {
                    foundCode = true;
                    yield return new ClassificationSpan(tag.Span, tag.Tag.ClassificationType);
                }
            }
            while (!foundCode && curLine.LineNumber > 0);
        }
        ///<summary>Consumes tokens from a classification enumerator until it rests at the specified token.</summary>
        ///<returns>False if no such token was found until the end of the enumerator.</returns>
        private static bool ConsumeToToken(IEnumerator<ClassificationSpan> enumerator, string token, string classificationType)
        {
            while (!(enumerator.Current.ClassificationType.IsOfType(classificationType)
                 && enumerator.Current.Span.GetText() == token))
                if (!enumerator.MoveNext())
                    return false;

            return true;
        }
        ///<summary>Consumes any comment tokens from a classification enumerator, until it rests at a non-comment token or at the end.</summary>
        ///<returns>False if the enumerator has ended (at a comment).</returns>
        private static bool SkipComments(IEnumerator<ClassificationSpan> enumerator)
        {
            while (enumerator.Current.ClassificationType.IsOfType("comment"))
                if (!enumerator.MoveNext())
                    return false;
            return true;
        }
    }
}
