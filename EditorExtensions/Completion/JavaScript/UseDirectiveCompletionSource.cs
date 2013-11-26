using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor;
using Intel = Microsoft.VisualStudio.Language.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    class UseDirectiveCompletionSource : StringCompletionSource
    {
        public override Span? GetInvocationSpan(string text, int linePosition)
        {
            var quote = text.SkipWhile(Char.IsWhiteSpace).FirstOrDefault();
            if (quote != '"' && quote != '\'')
                return null;

            var startIndex = text.TakeWhile(Char.IsWhiteSpace).Count();
            var endIndex = linePosition;

            // Consume the auto-added close quote, if present.
            // If range ends at the end of the line, we cannot
            // check this.
            if (endIndex < text.Length && text[endIndex] == quote)
                endIndex++;
            return Span.FromBounds(startIndex, endIndex);
        }

        static ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupIntrinsic, StandardGlyphItem.GlyphItemPublic);
        public override IEnumerable<Intel.Completion> GetEntries(char quoteChar, SnapshotPoint caret)
        {
            return new[] { "use strict", "use asm" }.Select(s => new Intel.Completion(
                quoteChar + s + quoteChar + ";",
                quoteChar + s + quoteChar + ";",
                "Instructs that this block be processed in " + s.Substring(4) + " mode by supporting JS engines",
                _glyph,
                null)
            );
        }
    }
}
