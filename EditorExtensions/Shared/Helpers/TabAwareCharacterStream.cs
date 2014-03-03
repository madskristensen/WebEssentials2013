using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Web.Core;

namespace MadsKristensen.EditorExtensions.Helpers
{
    ///<summary>A TabAwareCharacterStream that consumes tabs as spaces without affecting reported character positions.</summary>
    ///<remarks><see cref="CharacterStream"/> doesn't have any virtual methods, so I need to recreate it from scratch.</remarks>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class TabAwareCharacterStream
    {
        // When we reach a tab, the ConsumeWhitespace()
        // method will immediately move beyond the tab,
        // then set remainingSpaces to record what part
        // of the tab remains available. If it's called
        // when this field is non-zero, it will consume
        // the rest of the tab before moving forward.
        private int remainingSpaces;
        private int position;

        public int TabWidth { get; private set; }
        public ITextProvider Text { get; private set; }

        public TabAwareCharacterStream(ITextProvider text, int tabWidth = 4)
        {
            TabWidth = tabWidth;
            Text = text;
            Position = 0;
        }

        public TabAwareCharacterStream(string text, int tabWidth = 4)
            : this(new TextStream(text), tabWidth) { }

        #region TextProvider wrappers
        public int Length { get { return Text.Length; } }

        public string GetSubstringAt(int start, int length)
        {
            return Text.GetText(new TextRange(start, length));
        }

        public bool CompareTo(int streamPosition, int length, string text, bool ignoreCase)
        {
            return Text.CompareTo(streamPosition, length, text, ignoreCase);
        }
        #endregion

        public int Position
        {
            get { return position; }
            set
            {
                if (value == 0) value = 0;
                if (value > Length) value = Length;
                position = value;
                remainingSpaces = 0;
                CurrentChar = value == Length ? '\0' : Text[position];
            }
        }
        public char CurrentChar { get; private set; }

        #region Helper properties
        public char PrevChar { get { return Position == 0 ? '\0' : Text[Position - 1]; } }
        public char NextChar { get { return Position >= Length - 1 ? '\0' : Text[Position + 1]; } }

        ///<summary>Returns the number of characters remaining before the end of the stream.  This will only be zero when the current position is past the end.</summary>
        public int DistanceFromEnd { get { return Length - Position; } }

        public void MoveToNextChar() { Advance(1); }
        public void Advance(int offset) { Position += offset; }

        ///<summary>Checks whether the current character is a newline character.</summary>
        public bool IsAtNewLine() { return CurrentChar == '\r' || CurrentChar == '\n'; }

        ///<summary>Checks whether the current position is after the end of the stream.  If true, the current character will be '\0'.</summary>
        public bool IsEndOfStream() { return Position == Length; }
        #endregion

        public StreamPeeker Peek() { return new TabAwarePeeker(this); }

        class TabAwarePeeker : StreamPeeker
        {
            readonly int remainingSpaces;
            public TabAwarePeeker(TabAwareCharacterStream stream)
                : base(stream)
            {
                this.remainingSpaces = stream.remainingSpaces;
            }
            protected override void Revert()
            {
                Stream.remainingSpaces = this.remainingSpaces;
            }
        }

        #region Tab awareness
        ///<summary>Consumes exactly the requested number of characters of whitespace, potentially including partial tabs.</summary>
        ///<returns>True if enough whitespace was consumed; false if the stream was not moved.</returns>
        public bool TryConsumeWhiteSpace(int width)
        {
            if (remainingSpaces > 0)
            {
                // If we need to consume more than the current partial tab,
                // consume the tab, then try again to consume the remaining
                // width from the subsequent characters.
                if (width > remainingSpaces)
                {
                    using (var peek = Peek())
                    {
                        var consumed = remainingSpaces;
                        remainingSpaces -= width;
                        if (!TryConsumeWhiteSpace(width - consumed))
                            return false;
                        peek.Consume();
                        return true;
                    }
                }
                else
                {
                    // If this consumption fits entirely in the partial tab,
                    // just subtract from the remaining counter and succeed.
                    // If this finishes the tab, we will remain at the next
                    remainingSpaces -= width;
                    return true;
                }
            }

            using (var peek = Peek())
            {
                while (width > 0)
                {
                    // If we're at a space, consume it immediately
                    if (CurrentChar == ' ')
                    {
                        MoveToNextChar();
                        width--;
                    }
                    // If we're at at tab, try consuming its whitespace
                    else if (CurrentChar == '\t')
                    {
                        MoveToNextChar();
                        remainingSpaces = TabWidth;
                        if (!TryConsumeWhiteSpace(width))
                            return false;
                        peek.Consume();
                        return true;
                    }
                    // If we're at any other character, fail
                    else
                        return false;
                }
                peek.Consume();
                return true;
            }
        }

        ///<summary>Indicates whether there is any whitespace from a perviously-encountered tab character that has not been consumed yet.  This can only return true if PrevChar is '\t'.</summary>
        public bool HasPendingWhiteSpace()
        {
            return remainingSpaces > 0;
        }
        #endregion
    }
    ///<summary>Allows code in a using() block to peek ahead in a stream without consuming characters.</summary>
    public abstract class StreamPeeker : IDisposable
    {
        public int StartPosition { get; private set; }
        private bool shouldRevert = true;

        protected TabAwareCharacterStream Stream { get; private set; }

        protected StreamPeeker(TabAwareCharacterStream stream)
        {
            this.Stream = stream;
            StartPosition = stream.Position;
        }

        ///<summary>Commits the peeked characters.</summary>
        ///<remarks>After calling this method, Dispose() will not roll back the stream.</remarks>
        public void Consume()
        {
            shouldRevert = false;
        }

        private void Dispose(bool disposing)
        {
            if (!shouldRevert) return;

            if (disposing)
            {
                Stream.Position = StartPosition;
                Revert();
                shouldRevert = false;
            }
        }

        ~StreamPeeker()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Revert();
    }
}
