using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Html.Core;
using Microsoft.Web.Core;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    // To work properly with the HTML editor, we must return an
    // Artifact as soon as the user types the start characters.
    // This means that we must always report empty code blocks,
    // including empty lines inside code blocks, as zero-length
    // artifacts.
    // See MarkdownCodeArtifactCollection.IsDestructiveChange()
    // which checks for "typing the start characters".  It will
    // only look for new Artifacts when that returns true.

    public class MarkdownParser
    {
        readonly CharacterStream stream;

        public MarkdownParser(CharacterStream stream)
        {
            this.stream = stream;
        }

        ///<summary>Occurs when an artifact is found.</summary>
        public event EventHandler<MarkdownArtifactEventArgs> ArtifactFound;
        ///<summary>Raises the ArtifactFound event.</summary>
        ///<param name="e">A  ArtifactEventArgs object that provides the event data.</param>
        internal protected virtual void OnArtifactFound(MarkdownArtifactEventArgs e)
        {
            if (ArtifactFound != null)
                ArtifactFound(this, e);
        }
        // CharacterStream cannot overflow; setting Position
        // past the end will move the the end and return \0.
        // The parsing logic is based on GitHub experiments.

        private abstract class ParserBase
        {

            protected readonly CharacterStream stream;
            protected readonly Action<MarkdownCodeArtifact> ReportArtifact;
            protected ParserBase(CharacterStream stream, Action<MarkdownCodeArtifact> reporter)
            {
                this.stream = stream;
                this.ReportArtifact = reporter;
            }
            protected StreamPeeker Peek() { return new StreamPeeker(stream); }

            protected void SkipSpaces(int max)
            {
                for (int i = 0; i < max; i++)
                {
                    if (!TryReadSpaces(1))
                        break;
                }
            }

            ///<summary>Consumes exactly the requested number of characters of whitespace, potentially including partial tabs.</summary>
            ///<returns>True if enough whitespace was consumed; false if the stream was not moved.</returns>
            protected bool TryReadSpaces(int count)
            {
                using (var peek = Peek())
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (stream.CurrentChar == '\t')
                        {
                            // TODO: Tab equivalence in TabAwareCharacterStream class
                        }
                        else if (stream.CurrentChar != ' ')
                            return false;
                        stream.MoveToNextChar();
                    }
                    peek.Consume();
                    return true;
                }
            }
            protected void SkipToEndOfLine()
            {
                while (!stream.IsEndOfStream() && !stream.IsAtNewLine())
                    stream.MoveToNextChar();
            }
            protected bool TryConsumeNewLine()
            {
                var start = stream.Position;
                if (stream.CurrentChar == '\r')
                    stream.MoveToNextChar();
                if (stream.CurrentChar == '\n')
                    stream.MoveToNextChar();
                return stream.Position != start;
            }
            protected void SkipToNextLine()
            {
                SkipToEndOfLine();
                TryConsumeNewLine();
            }
            protected bool TrySkipBlankLine()
            {
                using (var peek = Peek())
                {
                    while (stream.CurrentChar == ' ' || stream.CurrentChar == '\t' || stream.CurrentChar == '\f' || stream.CurrentChar == '\u200B')
                        stream.MoveToNextChar();
                    if (!TryConsumeNewLine())
                        return false;

                    peek.Consume();
                    return true;
                }
            }

            protected bool TryConsume(string expected)
            {
                if (!stream.CompareCurrent(expected))
                    return false;
                stream.Advance(expected.Length);
                return true;
            }
        }
        private abstract class BlockParser : ParserBase
        {
            private int quoteDepth;

            protected BlockParser(CharacterStream stream, Action<MarkdownCodeArtifact> reporter) : base(stream, reporter) { }

            private int ReadBlockQuotePrefix(int? maxDepth = null)
            {
                if (maxDepth == 0) return 0;
                using (var peek = Peek())
                {
                    // TODO: Add more accurate logic for pathological mixes of tabs and spaces in nested quotes & code blocks
                    if (!TryConsume("\t")) SkipSpaces(3);
                    // If we didn't find a > at the beginning, don't consume anything.
                    if (stream.CurrentChar != '>')
                        return 0;
                    stream.MoveToNextChar();
                    SkipSpaces(1);  // A single space following the > is consumed as part of the prefix, and doesn't count for anything else.
                    peek.Consume();
                    // If we did consume a quote, look for another one.
                    return 1 + ReadBlockQuotePrefix(maxDepth - 1);
                }
            }

            ///<summary>Consumes lines matching this block type.</summary>
            ///<returns>True if anything was consumed.</returns>
            ///<remarks>
            /// If the stream is not up to a block of this type, the stream
            /// will not be advanced.  Otherwise, it will be advanced until
            /// the first character following the newline after the block.
            ///</remarks>
            public bool Parse()
            {
                if (stream.IsAtLastCharacter()) return false;

                if (stream.Position != 0 && stream.PrevChar != '\n')
                    throw new InvalidOperationException("BlockParser must be called at the first character in a block.");
                using (var peek = Peek())
                {
                    quoteDepth = ReadBlockQuotePrefix();

                    // Skip any empty lines before the content.
                    while (TrySkipBlankLine())
                    {
                        // If the quotes got deeper, consume an empty block, so
                        // we can start the next one at the right nesting level
                        if (CheckDeeperQuote())
                        {
                            // Consume the leading blank lines, but not the beginning of the deeper quote.
                            peek.Consume();
                            return true;
                        }
                        ReadBlockQuotePrefix(quoteDepth);
                    }
                    if (!ReadContent())
                        return false;
                    if (stream.Position != 0 && !stream.IsAtLastCharacter() && stream.PrevChar != '\n')
                        throw new InvalidOperationException("ReadContent() must end at the first character in the next block.");
                    peek.Consume();
                    return true;
                }
            }

            protected abstract bool ReadContent();

            protected bool BlockEnded { get; private set; }
            ///<summary>Moves to the next content character inside the block (after quote prefixes).</summary>
            ///<returns>False if the block had ended (the stream will be up to the next block).</returns>
            protected bool MoveToNextContentChar()
            {
                // CharacterStream can go past the end of the stream,
                // setting CurrentChar equal to '\0', and Position ==
                // Length.  This state is not a content character; we
                // stop at the last valid CurrentChar.
                if (stream.IsAtLastCharacter() || BlockEnded)
                    return false;
                stream.MoveToNextChar();
                // If we're still in the middle of a line, return the character
                if (!TryConsumeNewLine())
                    return true;

                // If we find a blank line, we've reached a block boundary.
                if (TryConsumeEnd())
                {
                    BlockEnded = true;
                    return false;
                }
                using (var peek = Peek())
                {
                    if (TryConsumeLinePrefix())
                        peek.Consume();
                    else
                    {
                        // If we reached a line that doesn't have the right
                        // prefix, the block has ended.
                        BlockEnded = true;
                        return false;
                    }
                }
                return true;
            }

            ///<summary>Tries to consume the prefix (if any) expected before the content of each subsequent line in this block.</summary>
            ///<returns>
            /// True if the prefix has been consumed; false if this line is lacking the prefix
            /// If this function returns false, MoveToNextContentChar() will rewind the stream
            /// and terminate the block.
            /// This is not called for the beginning of the block.
            ///</returns>
            protected virtual bool TryConsumeLinePrefix()
            {
                ReadBlockQuotePrefix(quoteDepth);
                return true;
            }

            ///<summary>Tries to consume characters (immediately following a newline) that indicate the end of this block.</summary>
            ///<returns>True if the block has ended (the stream will be up to the next block); false if it has not (the stream will not have moved).</returns>
            protected virtual bool TryConsumeEnd()
            {
                // If the quotes got deeper, we've started a new block
                if (CheckDeeperQuote()) return true;
                using (var peek = Peek())
                {
                    ReadBlockQuotePrefix(quoteDepth);
                    if (!TrySkipBlankLine())
                        return false;
                    peek.Consume();
                    return true;
                }
            }
            ///<summary>Tries to consume content characters (after line prefixes) that indicate the end of this block.</summary>
            protected virtual bool TryConsumeEndContent()
            {
                return TrySkipBlankLine();
            }
            ///<summary>Checks whether this line begins with a quote prefix deeper than the rest of the block.</summary>
            ///<returns>True if this line should begin a new block.</returns>
            ///<remarks>This method will not advance the stream.</remarks>
            private bool CheckDeeperQuote()
            {
                if (stream.PrevChar != '\n')
                    throw new InvalidOperationException("CheckDeeperQuote() must be called at the first character in a line.");

                using (Peek())
                {
                    ReadBlockQuotePrefix(quoteDepth);
                    if (ReadBlockQuotePrefix() > 0)
                        return true;
                }
                return false;
            }

            ///<summary>Reads all content characters until the end of the current line or block.</summary>
            ///<returns>The range of content read, or null if the stream is at the end of the block.</returns>
            protected TextRange TryConsumeContentLine()
            {
                int contentStart = stream.Position;
                int contentEnd = -1;
                bool hitLineEnd = false;
                while (!hitLineEnd)
                {
                    contentEnd = stream.Position;

                    // If we've hit the end of the line, consume the current character and stop.
                    // (with the same logic in case we hit the end of the block too)
                    if (stream.NextChar == '\r' || stream.NextChar == '\n')
                        hitLineEnd = !BlockEnded;
                    // If we hit the end of the block, stop.

                    if (!MoveToNextContentChar())
                    {
                        // If we started at the end of the block, we didn't read anything.
                        if (contentStart == contentEnd && !hitLineEnd)
                            return null;
                        break;
                    }
                }
                // If we rested on a final character before ending the block / line,
                // include that character in the range.  If the line was empty, stay
                // with a zero-length range.
                if (contentStart != contentEnd)
                    contentEnd++;
                return TextRange.FromBounds(contentStart, contentEnd);
            }
        }

        class ContentBlockParser : BlockParser
        {
            public ContentBlockParser(CharacterStream stream, Action<MarkdownCodeArtifact> reporter) : base(stream, reporter) { }

            void ReadInlineCodeBlock()
            {
                using (var peek = Peek())
                {
                    stream.MoveToNextChar();
                    // Report `` as an empty code block; otherwise, the artifact isn't detected properly.
                    //if (stream.CurrentChar == '`')
                    //    return;
                    while (stream.CurrentChar != '`')
                    {
                        if (stream.IsAtNewLine() || stream.IsEndOfStream()) return;
                        stream.MoveToNextChar();
                    }

                    ReportArtifact(new MarkdownCodeArtifact(null, TextRange.FromBounds(peek.StartPosition, stream.Position + 1), 1, 1));
                    peek.Consume();
                }
            }

            protected override bool ReadContent()
            {
                do
                    if (stream.CurrentChar == '`' && stream.PrevChar != '\\')
                        ReadInlineCodeBlock();
                while (MoveToNextContentChar());
                return true;
            }

            protected override bool TryConsumeEnd()
            {
                using (Peek())
                {
                    TryConsumeLinePrefix();
                    SkipSpaces(3);
                    if (TryConsume("~~~") || TryConsume("```"))
                        return true;
                }
                return base.TryConsumeEnd();
            }
        }

        class IndentedCodeBlockParser : BlockParser
        {
            public IndentedCodeBlockParser(CharacterStream stream, Action<MarkdownCodeArtifact> reporter) : base(stream, reporter) { }

            // TODO: Detect numbered list and require 8 spaces
            int spaceCount = 4;

            // Holds the position of the beginning of the line's indent.
            // This is set when we consume a line prefix, then read when
            // reporting a code line.
            int lineStart;
            protected override bool ReadContent()
            {
                if (!TryReadSpaces(spaceCount))
                    return false;
                while (true)
                {
                    var thisLineStart = lineStart;
                    var range = TryConsumeContentLine();
                    if (range == null)
                        break;
                    // Get the character count of the indent, which may be different if tabs are involved.
                    var indentSize = range.Start - thisLineStart;
                    range.Expand(-indentSize, 0);
                    ReportArtifact(new MarkdownCodeArtifact(null, range, indentSize, 0));
                }
                return true;
            }
            protected override bool TryConsumeLinePrefix()
            {
                if (!base.TryConsumeLinePrefix())
                    return false;
                lineStart = stream.Position;
                return TryReadSpaces(spaceCount);
            }
        }
        class FencedCodeBlockParser : BlockParser
        {
            public FencedCodeBlockParser(CharacterStream stream, Action<MarkdownCodeArtifact> reporter) : base(stream, reporter) { }

            string fence;

            protected override bool ReadContent()
            {
                string language = null;

                if (TryConsume("```"))
                    fence = "```";
                else if (TryConsume("~~~"))
                    fence = "~~~";
                else
                    return false;

                var langRange = TryConsumeContentLine();
                if (langRange != null)
                    language = stream.GetSubstringAt(langRange.Start, langRange.Length);

                while (true)
                {
                    var range = TryConsumeContentLine();
                    if (range == null)
                    {
                        // If the fenced code block ends in an empty line at
                        // the end of the document, report an empty Artifact
                        // so that it can expand as the user types.  We need
                        // this ugly hack because MoveToNextContentChar will
                        // consume the newline and hit the end immediately.
                        if (stream.IsEndOfStream() && stream.PrevChar == '\n')
                            ReportArtifact(new MarkdownCodeArtifact(null, new TextRange(stream.Position - 1, 0), 0, 0));
                        break;
                    }
                    ReportArtifact(new MarkdownCodeArtifact(language, range, 0, 0));
                }
                return true;
            }
            protected override bool TryConsumeEnd()
            {
                using (var peek = Peek())
                {
                    TryConsumeLinePrefix();
                    if (!TryConsume(fence))
                        return false;
                    if (!stream.IsAtLastCharacter() && !TrySkipBlankLine())    // If there is any content after the fence, the block did not end.
                        return false;
                    peek.Consume();
                    return true;
                }
            }
        }

        public void Parse()
        {
            Action<MarkdownCodeArtifact> reporter = a => OnArtifactFound(new MarkdownArtifactEventArgs(a));
            while (!stream.IsAtLastCharacter())
            {
                // As soon as a parse succeeds, try all parsers again, so that
                // that the next block is parsed with the correct priorities.
                if (new IndentedCodeBlockParser(stream, reporter).Parse())
                    continue;
                if (new FencedCodeBlockParser(stream, reporter).Parse())
                    continue;
                if (new ContentBlockParser(stream, reporter).Parse())
                    continue;
            }
        }
    }
    ///<summary>Allows code in a using() block to peek ahead in a stream without consuming characters.</summary>
    public class StreamPeeker : IDisposable
    {
        public int StartPosition { get; private set; }
        readonly CharacterStream stream;
        private bool shouldRevert = true;

        public StreamPeeker(CharacterStream stream)
        {
            this.stream = stream;
            this.StartPosition = stream.Position;
        }
        ///<summary>Commits the peeked characters.</summary>
        ///<remarks>After calling this method, Dispose() will not roll back the stream.</remarks>
        public void Consume()
        {
            shouldRevert = false;
        }
        public void Dispose()
        {
            if (!shouldRevert) return;
            stream.Position = StartPosition;
            shouldRevert = false;
        }
    }

    public class MarkdownCodeArtifact : Artifact
    {
        public MarkdownCodeArtifact(string language, ITextRange range, int leftLength, int rightLength)
            : base(ArtifactTreatAs.Code, range, leftLength, rightLength, MarkdownClassificationTypes.MarkdownCode, true) { Language = language; }
        public string Language { get; private set; }
    }

    ///<summary>Provides data for Artifact events.</summary>
    public class MarkdownArtifactEventArgs : EventArgs
    {
        ///<summary>Creates a new MarkdownArtifactEventArgs instance.</summary>
        public MarkdownArtifactEventArgs(MarkdownCodeArtifact artifact) { Artifact = artifact; }

        ///<summary>Gets the artifact.</summary>
        public MarkdownCodeArtifact Artifact { get; private set; }
    }
}
