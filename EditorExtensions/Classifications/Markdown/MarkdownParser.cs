using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Html.Core;
using Microsoft.Web.Core;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
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
        private StreamPeeker Peek() { return new StreamPeeker(stream); }

        /*
         * Useful testcase (for GitHub)
a ` b `
2 `` 3

 1. abc  
def
        code!
 2. ghi

>>>>>>>>
> >     a
>     a
>    >    a```
>     iiii
>    aaa

 ```
vbbbb
```               a

After!
         */
        void SkipSpaces(int max)
        {
            for (int i = 0; i < max; i++)
            {
                if (stream.CurrentChar != ' ')
                    break;
                stream.MoveToNextChar();
            }
        }
        void SkipToEndOfLine()
        {
            while (!stream.IsEndOfStream() && !stream.IsAtNewLine())
                stream.MoveToNextChar();
        }
        void SkipToNextLine()
        {
            SkipToEndOfLine();
            while (!stream.IsEndOfStream() && stream.IsAtNewLine())
                stream.MoveToNextChar();    // Consume the newline (Windows or Linux)
        }
        bool SkipEmptyLine()
        {
            bool retVal = false;
            using (var peek = Peek())
            {
                while (stream.IsWhiteSpace())
                    stream.MoveToNextChar();
                while (!stream.IsEndOfStream() && stream.IsAtNewLine())
                {
                    stream.MoveToNextChar();    // Consume the newline (Windows or Linux)
                    peek.Consume();
                    retVal = true;
                }
            }
            return retVal;
        }

        bool TryConsume(string expected)
        {
            if (!stream.CompareCurrent(expected))
                return false;
            stream.Advance(expected.Length);
            return true;
        }

        int ReadBlockQuotePrefix(int? maxDepth = null)
        {
            if (maxDepth == 0) return 0;
            using (var peek = Peek())
            {
                SkipSpaces(3);
                // If we didn't find a > at the beginning, don't consume anything.
                if (stream.CurrentChar != '>')
                    return 0;

                SkipSpaces(1);  // A single space following the > is consumed as part of the prefix, and doesn't count for anything else.
                peek.Consume();
                // If we did consume a quote, look for another one.
                return 1 + ReadBlockQuotePrefix(maxDepth - 1);
            }
        }

        void ReadInlineCodeBlock()
        {
            using (var peek = Peek())
            {
                stream.MoveToNextChar();
                if (stream.CurrentChar == '`')  // `` is not a code block
                    return;
                while (stream.CurrentChar != '`')
                {
                    if (stream.IsAtNewLine() || stream.IsEndOfStream()) return;
                    stream.MoveToNextChar();
                }

                OnArtifactFound(new MarkdownArtifactEventArgs(new MarkdownCodeArtifact(null, TextRange.FromBounds(peek.StartPosition, stream.Position + 1), 1, 1)));
                peek.Consume();
            }
        }
        bool TryReadFencedCodeBlock()
        {
            using (var peek = Peek())
            {
                var quoteDepth = ReadBlockQuotePrefix();
                SkipSpaces(3);
                if (stream.CurrentChar != '`' && stream.CurrentChar != '~')
                    return false;
                string fence = new string(stream.CurrentChar, 3);

                if (!TryConsume(fence))
                    return false;
                //TODO: Look for comment- or tag- based language prefixes in line before StartPosition

                StringBuilder language = new StringBuilder();
                while (!stream.IsEndOfStream() && !stream.IsAtNewLine())
                {
                    language.Append(stream.CurrentChar);
                    stream.MoveToNextChar();
                }
                while (stream.IsAtNewLine()) stream.MoveToNextChar();    // Consume the newline

                // Keep reading entire lines until we find a closing fence.
                while (!stream.IsEndOfStream())
                {
                    ReadBlockQuotePrefix(maxDepth: quoteDepth);
                    using (var endFencePeek = Peek())
                    {
                        SkipSpaces(3);

                        if (TryConsume(fence) && SkipEmptyLine())
                        {
                            endFencePeek.Consume();
                            break;
                        }
                    }

                    var lineStart = stream.Position;
                    SkipToEndOfLine();
                    OnArtifactFound(new MarkdownArtifactEventArgs(new MarkdownCodeArtifact(
                        language.ToString().Trim(),
                        TextRange.FromBounds(lineStart, stream.Position),
                        0, 0
                    )));
                    while (stream.IsAtNewLine()) stream.MoveToNextChar();    // Consume the newline
                }

                peek.Consume();
                return true;
            }
        }
        void ReadIndentedCodeBlock()
        {
            // This method will always exit when the stream is at a newline
            using (var peek = Peek())
            {

                var quoteDepth = ReadBlockQuotePrefix();
                if (!SkipEmptyLine())
                    return;    // Indented code block must be preceded by blank line.

                // TODO: Detect numbered list and require 8 spaces
                string prefix = new string(' ', 4);


                // Keep reading entire lines until we find a closing fence.
                while (!stream.IsEndOfStream())
                {
                    // If we don't find indentation, don't consume the newline or quote block
                    using (var prefixPeek = Peek())
                    {
                        while (stream.IsAtNewLine())
                            stream.MoveToNextChar();    // Consume the newline from the previous line
                        ReadBlockQuotePrefix(maxDepth: quoteDepth);
                        if (!TryConsume(prefix))
                            break;
                        prefixPeek.Consume();   // Consume the indentation and read the content.
                    }
                    var lineStart = stream.Position;
                    SkipToEndOfLine();
                    OnArtifactFound(new MarkdownArtifactEventArgs(new MarkdownCodeArtifact(
                        null,
                        TextRange.FromBounds(lineStart, stream.Position),
                        0, 0
                    )));
                }

                peek.Consume();
            }
        }

        public void Parse()
        {
            while (!stream.IsEndOfStream())
            {
                switch (stream.CurrentChar)
                {
                    case '`':
                        ReadInlineCodeBlock();
                        break;
                    case '\n':
                        ReadIndentedCodeBlock();    // Always leaves stream at newline
                        if (TryReadFencedCodeBlock())
                            stream.MoveToNextChar();
                        break;
                }
                stream.MoveToNextChar();
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
            : base(ArtifactTreatAs.Code, range, leftLength, rightLength, language, true) { }
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
