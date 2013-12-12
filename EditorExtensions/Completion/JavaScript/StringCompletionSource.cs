using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Intel = Microsoft.VisualStudio.Language.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    public abstract class StringCompletionSource
    {
        ///<summary>Gets the span within a line of text that this source should provide completion for, or null if their is no completions for the caret position.</summary>
        public abstract Span? GetInvocationSpan(string text, int linePosition, SnapshotPoint position);

        ///<summary>Gets the completion entries for the specified quoted string.</summary>
        public abstract IEnumerable<Intel.Completion> GetEntries(char quote, SnapshotPoint caret);
    }

    ///<summary>A StringCompletionSource that provides completions for the parameter to a specific function call.</summary>
    ///<remarks>This does not yet support multiple parameters.</remarks>
    public abstract class FunctionCompletionSource : StringCompletionSource
    {
        protected abstract string FunctionName { get; }

        public override Span? GetInvocationSpan(string text, int linePosition, SnapshotPoint position)
        {
            // Find the quoted string inside function call
            int startIndex = text.LastIndexOf(FunctionName + "(", linePosition, StringComparison.Ordinal);
            if (startIndex < 0)
                return null;
            startIndex += FunctionName.Length + 1;
            startIndex += text.Skip(startIndex).TakeWhile(Char.IsWhiteSpace).Count();

            if (linePosition <= startIndex || (text[startIndex] != '"' && text[startIndex] != '\''))
                return null;

            var endIndex = text.IndexOf(text[startIndex] + ")", startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex < 0)
                endIndex = startIndex + text.Skip(startIndex + 1).TakeWhile(c => Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c) || c == '-' || c == '_').Count() + 1;
            else if (linePosition > endIndex + 1)
                return null;

            // Consume the auto-added close quote, if present.
            // If range ends at the end of the line, we cannot
            // check this.
            if (endIndex < text.Length && text[endIndex] == text[startIndex])
                endIndex++;


            return Span.FromBounds(startIndex, endIndex);
        }
    }
}