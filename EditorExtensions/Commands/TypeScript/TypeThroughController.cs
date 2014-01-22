using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    public class TypeThroughController : IIntellisenseController
    {
        readonly ITextBuffer _textBuffer;
        readonly ITextView _textView;

        readonly List<ProvisionalText> _provisionalTexts = new List<ProvisionalText>();
        char _typedChar = '\0';
        bool _processing = false;
        int _caretPosition = 0;
        int _bufferVersionWaterline;

        public TypeThroughController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            _textBuffer = subjectBuffers[0];
            _textView = textView;

            _textBuffer.Changed += TextBuffer_Changed;
            _textBuffer.PostChanged += TextBuffer_PostChanged;

            _bufferVersionWaterline = _textBuffer.CurrentSnapshot.Version.ReiteratedVersionNumber;
        }

        protected virtual bool CanComplete(ITextBuffer textBuffer, int position)
        {
            return true;
        }

        void TextBuffer_PostChanged(object sender, System.EventArgs e)
        {
            if (!_processing && _typedChar != '\0')
            {
                OnPostTypeChar(_typedChar);
                _typedChar = '\0';
            }
        }

        void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            if (_processing)
                return;

            _typedChar = '\0';

            if (e.Changes.Count == 1 && e.AfterVersion.ReiteratedVersionNumber > _bufferVersionWaterline)
            {
                var change = e.Changes[0];

                _bufferVersionWaterline = e.AfterVersion.ReiteratedVersionNumber;

                // Change length may be > 1 in autoformatting languages.
                // However, there will be only one non-ws character in the change.
                // Be careful when </script> is inserted: the change won't
                // actually be in this buffer.

                var snapshot = _textBuffer.CurrentSnapshot;
                if (change.NewSpan.End <= snapshot.Length)
                {
                    var text = _textBuffer.CurrentSnapshot.GetText(change.NewSpan);
                    text = text.Trim();

                    if (text.Length == 1)
                    {
                        // Allow completion of different characters inside spans, but not when
                        // character and its completion pair is the same. For example, we do
                        // want to complete () in foo(bar|) when user types ( after bar. However,
                        // we do not want to complete " when user is typing in a string which
                        // was already completed and instead " should be a terminating type-through.

                        var typedChar = text[0];
                        var completionChar = GetCompletionCharacter(typedChar);

                        var caretPosition = GetCaretPositionInBuffer();
                        if (caretPosition.HasValue)
                        {
                            bool compatible = true;

                            var innerText = GetInnerProvisionalText();
                            if (innerText != null)
                                compatible = IsCompatibleCharacter(innerText.ProvisionalChar, typedChar);

                            if (!IsPositionInProvisionalText(caretPosition.Value) || typedChar != completionChar || compatible)
                            {
                                _typedChar = typedChar;
                                _caretPosition = caretPosition.Value;
                            }
                        }
                    }
                }
            }
        }

        protected virtual char GetCompletionCharacter(char typedCharacter)
        {
            switch (typedCharacter)
            {
                case '{':
                case '}':
                    return '}';
            }

            return '\0';
        }

        protected virtual bool IsCompatibleCharacter(char primaryCharacter, char candidateCharacter)
        {
            if (primaryCharacter == '\"' || primaryCharacter == '\'')
                return false; // no completion in strings

            return true;
        }

        private void OnPostTypeChar(char typedCharacter)
        {
            // When language autoformats, like JS, caret may be in a very different
            // place by now. Check if store caret position still makes sense and
            // if not, reacquire it. In contained language scenario
            // current caret position may be beyond projection boundary like when
            // typing at the end of onclick="return foo(".

            if (WESettings.Instance.TypeScript.BraceCompletion)
            {
                char completionCharacter = GetCompletionCharacter(typedCharacter);
                if (completionCharacter != '\0')
                {
                    var viewCaretPosition = _textView.Caret.Position.BufferPosition;
                    _processing = true;

                    var bufferCaretPosition = GetCaretPositionInBuffer();
                    if (bufferCaretPosition.HasValue)
                    {
                        _caretPosition = bufferCaretPosition.Value;
                    }
                    else if (viewCaretPosition.Position == _textView.TextBuffer.CurrentSnapshot.Length)
                    {
                        _caretPosition = _textBuffer.CurrentSnapshot.Length;
                    }

                    if (_caretPosition > 0)
                    {
                        if (CanComplete(_textBuffer, _caretPosition - 1))
                        {
                            ProvisionalText.IgnoreChange = true;
                            _textView.TextBuffer.Replace(new Span(viewCaretPosition, 0), completionCharacter.ToString());
                            ProvisionalText.IgnoreChange = false;

                            _textView.Caret.MoveTo(new SnapshotPoint(_textView.TextBuffer.CurrentSnapshot, viewCaretPosition));

                            var provisionalText = new ProvisionalText(_textView, new Span(viewCaretPosition - 1, 2));
                            provisionalText.Closing += OnCloseProvisionalText;

                            _provisionalTexts.Add(provisionalText);
                        }
                    }
                }

                _processing = false;
            }
        }

        private SnapshotPoint? GetCaretPositionInBuffer()
        {
            var viewCaretPosition = _textView.Caret.Position.BufferPosition.Position;
            var snapshot = _textView.TextBuffer.CurrentSnapshot;

            if (viewCaretPosition > snapshot.Length)
                return null;

            return _textView.BufferGraph.MapDownToBuffer(
                    new SnapshotPoint(_textView.TextBuffer.CurrentSnapshot, viewCaretPosition), PointTrackingMode.Positive,
                    _textBuffer, PositionAffinity.Predecessor);
        }

        private bool IsPositionInProvisionalText(int position)
        {
            foreach (var pt in _provisionalTexts)
            {
                if (pt.IsPositionInSpan(position))
                    return true;
            }

            return false;
        }

        private ProvisionalText GetInnerProvisionalText()
        {
            int minLength = Int32.MaxValue;
            ProvisionalText innerText = null;

            foreach (var pt in _provisionalTexts)
            {
                if (pt.CurrentSpan.Length < minLength)
                {
                    minLength = pt.CurrentSpan.Length;
                    innerText = pt;
                }
            }

            return innerText;
        }

        private void OnCloseProvisionalText(object sender, EventArgs e)
        {
            _provisionalTexts.Remove(sender as ProvisionalText);
        }

        #region IIntellisenseController Members

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            if (_textBuffer != null)
            {
                _textBuffer.Changed += TextBuffer_Changed;
                _textBuffer.PostChanged += TextBuffer_PostChanged;
            }
        }

        public void Detach(ITextView textView)
        {
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            if (_textBuffer != null)
            {
                _textBuffer.Changed -= TextBuffer_Changed;
                _textBuffer.PostChanged -= TextBuffer_PostChanged;
            }
        }

        #endregion
    }
}