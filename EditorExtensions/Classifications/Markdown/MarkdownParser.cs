using System;
using MadsKristensen.EditorExtensions.Helpers;
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
        readonly TabAwareCharacterStream stream;

        public MarkdownParser(TabAwareCharacterStream stream)
        {
            this.stream = stream;
        }

        ///<summary>Occurs when an artifact is found.</summary>
        public event EventHandler<ArtifactEventArgs> ArtifactFound;
        ///<summary>Raises the ArtifactFound event.</summary>
        ///<param name="e">An ArtifactEventArgs object that provides the event data.</param>
        internal protected virtual void OnArtifactFound(ArtifactEventArgs e)
        {
            if (ArtifactFound != null)
                ArtifactFound(this, e);
        }
        // TabAwareCharacterStream cannot overflow; setting Position
        // past the end will move the the end and return \0.
        // The parsing logic is based on GitHub experiments.

        private abstract class ParserBase
        {
            protected readonly TabAwareCharacterStream stream;
            private readonly Action<Artifact> artifactReporter;
            protected ParserBase(TabAwareCharacterStream stream, Action<Artifact> reporter)
            {
                this.stream = stream;
                this.artifactReporter = reporter;
            }
            protected StreamPeeker Peek() { return stream.Peek(); }

            protected void ReportArtifact(CodeLineArtifact artifact)
            {
                artifact.BlockInfo.CodeLines.Add(artifact);
                artifactReporter(artifact);
            }
            protected void ReportArtifact(Artifact artifact)
            {
                artifactReporter(artifact);
            }
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
            protected bool TryReadSpaces(int count) { return stream.TryConsumeWhiteSpace(count); }

            protected bool TryConsumeNewLine()
            {
                var start = stream.Position;
                if (stream.CurrentChar == '\r')
                    stream.MoveToNextChar();
                if (stream.CurrentChar == '\n')
                    stream.MoveToNextChar();
                return stream.Position != start;
            }

            ///<summary>Tries to consume a line of whitespace characters.</summary>
            ///<param name="consumeCodeBlock">If false, this will not consume four or more spaces, to allow them to be parsed as an empty code block</param>
            ///<remarks>dontConsumeCodeBlock should only be false at the beginning of a line that may start a new block.</remarks>
            protected bool TrySkipBlankLine(bool consumeCodeBlock)
            {
                using (var peek = Peek())
                {
                    if (!consumeCodeBlock)
                        SkipSpaces(3);
                    else
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

            protected BlockParser(TabAwareCharacterStream stream, Action<Artifact> reporter) : base(stream, reporter) { }

            private int ReadBlockQuotePrefix(int? maxDepth = null)
            {
                if (maxDepth == 0) return 0;
                using (var peek = Peek())
                {
                    SkipSpaces(3);
                    // If there are more than three spaces before this arrow,
                    // it's actually an indented code block. This comes after
                    // the single space consumed by the previous quote block,
                    // if any.
                    if (stream.HasPendingWhiteSpace())
                        return 0;
                    // If we didn't find a > at the beginning, don't consume anything.
                    if (!TryConsume(">"))
                        return 0;
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

                if (stream.Position != 0 && stream.PrevChar != '\n' && stream.PrevChar != '\r')
                    throw new InvalidOperationException("BlockParser must be called at the first character in a block.");
                using (var peek = Peek())
                {
                    quoteDepth = ReadBlockQuotePrefix();

                    // Skip any empty lines before the content. (other than empty indented code lines)
                    while (TrySkipBlankLine(consumeCodeBlock: false))
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
                    if (stream.Position != 0 && !stream.IsAtLastCharacter() && stream.PrevChar != '\n' && stream.PrevChar != '\r')
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
                if (stream.IsAtLastCharacter())
                    BlockEnded = true;
                if (BlockEnded)
                    return false;
                // If we are at a regular character, consume it first. If we're
                // at a newline, run our separate newline handler for quotes.
                if (!stream.IsAtNewLine())
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
                    if (!TrySkipBlankLine(consumeCodeBlock: true))
                        return false;
                    peek.Consume();
                    return true;
                }
            }

            ///<summary>Checks whether this line begins with a quote prefix deeper than the rest of the block.</summary>
            ///<returns>True if this line should begin a new block.</returns>
            ///<remarks>This method will not advance the stream.</remarks>
            private bool CheckDeeperQuote()
            {
                if (stream.PrevChar != '\n' && stream.PrevChar != '\r')
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
                // If we already consumed the end of the block
                // (from the end of the last line), I will not
                // have anything more to read.
                if (BlockEnded) return null;

                int contentStart = stream.Position;
                int contentEnd = -1;
                bool hitLineEnd = false;
                while (!hitLineEnd)
                {
                    contentEnd = stream.Position;
                    // Unless we're already at the end of the line (unless the line
                    // is completely empty), the end is after this character.
                    if (!stream.IsAtNewLine() && !stream.IsEndOfStream())
                        contentEnd++;

                    // If we've hit the end of the line, consume the current character and stop.
                    // (with the same logic in case we hit the end of the block too)
                    if (stream.IsAtNewLine() || stream.NextChar == '\r' || stream.NextChar == '\n' || stream.IsEndOfStream())
                        hitLineEnd = !BlockEnded;
                    // If we hit the end of the block, stop.

                    if (!MoveToNextContentChar())
                    {
                        // If we have not consumed any characters, and
                        // we hit the end up the block without hitting
                        // a preceding newline first, do not return an
                        // empty line, since we actually simply got to
                        // the end of this block earlier.
                        if (contentStart == contentEnd && !hitLineEnd)
                            return null;
                        break;
                    }
                }
                return TextRange.FromBounds(contentStart, contentEnd);
            }
        }

        class ContentBlockParser : BlockParser
        {
            public ContentBlockParser(TabAwareCharacterStream stream, Action<Artifact> reporter) : base(stream, reporter) { }

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

                    ReportArtifact(new Artifact(
                        ArtifactTreatAs.Code,
                        TextRange.FromBounds(peek.StartPosition, stream.Position + 1),
                        1, 1,
                        MarkdownClassificationTypes.MarkdownCode, true
                    ));
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
            public IndentedCodeBlockParser(TabAwareCharacterStream stream, Action<Artifact> reporter) : base(stream, reporter) { }

            // TODO: Detect numbered list and require 8 spaces
            int spaceCount = 4;

            // Holds the position of the beginning of the line's indent.
            // This is set when we consume a line prefix, then read when
            // reporting a code line.
            int lineStart;
            protected override bool ReadContent()
            {
                var blockInfo = new CodeBlockInfo { OuterStart = new TextRange(stream.Position, 0) };
                lineStart = stream.Position;
                if (!TryReadSpaces(spaceCount))
                    return false;
                while (true)
                {
                    var thisLineStart = lineStart;
                    var range = TryConsumeContentLine();
                    if (range == null)
                        break;
                    blockInfo.OuterEnd = new TextRange(range.End, 0);
                    // Get the character count of the indent, which may be different if tabs are involved.
                    var indentSize = range.Start - thisLineStart;
                    range.Expand(-indentSize, 0);
                    ReportArtifact(new CodeLineArtifact(blockInfo, range, indentSize, 0));
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

            protected override bool TryConsumeEnd()
            {
                // If the next line is still a code block, we haven't ended
                // (even if it is otherwise a blank line, which would end a
                // content block).  In other words, a line with four spaces
                // ends a content block, but not a code block.
                using (var peek = Peek())
                    if (TryConsumeLinePrefix())
                        return false;
                return base.TryConsumeEnd();
            }
        }

        class FencedCodeBlockParser : BlockParser
        {
            public FencedCodeBlockParser(TabAwareCharacterStream stream, Action<Artifact> reporter) : base(stream, reporter) { }

            string fence;
            CodeBlockInfo blockInfo;
            protected override bool ReadContent()
            {
                SkipSpaces(3);
                var sepStart = stream.Position;
                if (TryConsume("```"))
                    fence = "```";
                else if (TryConsume("~~~"))
                    fence = "~~~";
                else
                    return false;

                blockInfo = new CodeBlockInfo { OuterStart = TextRange.FromBounds(sepStart, stream.Position) };

                var langRange = TryConsumeContentLine();
                if (langRange != null)
                    blockInfo.Language = stream.GetSubstringAt(langRange.Start, langRange.Length);

                while (true)
                {
                    var range = TryConsumeContentLine();
                    if (range == null)
                        break;
                    ReportArtifact(new CodeLineArtifact(blockInfo, range, 0, 0));
                }
                // If the stream ended without a closing fence, give an empty end.
                if (blockInfo.OuterEnd == null)
                    blockInfo.OuterEnd = new TextRange(stream.Length, 0);
                return true;
            }

            protected override bool TryConsumeEnd()
            {
                using (var peek = Peek())
                {
                    TryConsumeLinePrefix();
                    SkipSpaces(3);
                    var sepStart = stream.Position;
                    if (!TryConsume(fence))
                        return false;
                    blockInfo.OuterEnd = TextRange.FromBounds(sepStart, stream.Position);
                    if (!stream.IsAtLastCharacter() && !TrySkipBlankLine(consumeCodeBlock: true))    // If there is any content after the fence, the block did not end.
                        return false;
                    peek.Consume();
                    return true;
                }
            }
        }

        public void Parse()
        {
            Action<Artifact> reporter = a => OnArtifactFound(new ArtifactEventArgs(a));
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

    ///<summary>An Artifact containing a single line of code.</summary>
    public class CodeLineArtifact : Artifact, ICodeBlockArtifact
    {
        public CodeLineArtifact(CodeBlockInfo blockInfo, ITextRange range, int leftLength, int rightLength)
            : base(ArtifactTreatAs.Code, range, leftLength, rightLength, MarkdownClassificationTypes.MarkdownCode, true)
        {
            if (blockInfo == null) throw new ArgumentNullException("blockInfo");

            BlockInfo = blockInfo;
        }

        ///<summary>Gets information about the containing code block.</summary>
        public CodeBlockInfo BlockInfo { get; private set; }
    }
    ///<summary>An artifact associated with a code block.</summary>
    public interface ICodeBlockArtifact : IArtifact
    {
        CodeBlockInfo BlockInfo { get; }
    }
    ///<summary>Stores information about a complete code block, which may include multiple Artifacts.</summary>
    public class CodeBlockInfo
    {
        public CodeBlockInfo() { CodeLines = new TextRangeCollection<CodeLineArtifact>(); }

        ///<summary>Indicates whether the CodeLineArtifacts in this block have been removed from the general ArtifactCollection.</summary>
        public bool IsExtradited { get; set; }

        public TextRangeCollection<CodeLineArtifact> CodeLines { get; private set; }
        public string Language { get; set; }
        public ITextRange OuterStart { get; set; }
        public ITextRange OuterEnd { get; set; }
    }

    ///<summary>Provides data for Artifact events.</summary>
    public class ArtifactEventArgs : EventArgs
    {
        ///<summary>Creates a new ArtifactEventArgs instance.</summary>
        public ArtifactEventArgs(Artifact artifact) { Artifact = artifact; }

        ///<summary>Gets the artifact.</summary>
        public Artifact Artifact { get; private set; }
    }
}
