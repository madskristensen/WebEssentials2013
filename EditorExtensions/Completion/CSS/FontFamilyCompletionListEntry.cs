using System;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// This represents a font family in the completion list
    /// </summary>
    internal class FontFamilyCompletionListEntry : ICssCompletionListEntry
    {
        private string _name;

        public FontFamilyCompletionListEntry(string name)
        {
            _name = name ?? string.Empty;
        }

        public string DisplayText
        {
            get { return _name; }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public string GetSyntax(Version version)
        {
            return string.Empty;
        }

        public string GetAttribute(string name)
        {
            return string.Empty;
        }

        public string GetVersionedAttribute(string name, System.Version version)
        {
            return string.Empty;
        }

        public string GetInsertionText(CssTextSource textSource, ITrackingSpan typingSpan)
        {
            string text = DisplayText;
            bool needsQuote = text.IndexOf(' ') != -1;
            if (text == "Pick from file...")
            {
                return string.Empty;
            }

            if (needsQuote)
            {
                // Prefer to use single quotes, but if the inline style uses single quotes, then use double quotes.
                char quote = (textSource == CssTextSource.InlineStyleSingleQuote) ? '"' : '\'';

                if (typingSpan != null)
                {
                    // If the user already typed a quote, then use it

                    string typingText = typingSpan.GetText(typingSpan.TextBuffer.CurrentSnapshot);

                    if (!string.IsNullOrEmpty(typingText) && (typingText[0] == '"' || typingText[0] == '\''))
                    {
                        quote = typingText[0];
                    }
                }

                if (text != null && text.IndexOf(quote) == -1)
                {
                    text = quote.ToString() + text + quote.ToString();
                }
            }

            return text;
        }

        public StandardGlyphGroup StandardGlyph
        {
            get { return StandardGlyphGroup.GlyphGroupEnumMember; }
        }

        public bool AllowQuotedString
        {
            get { return true; }
        }

        public bool IsBuilder
        {
            get { return DisplayText == "Pick from file..."; }
        }

        public int SortingPriority
        {
            get { return 0; }
        }


        public bool IsSupported(BrowserVersion browser)
        {
            return true;
        }

        public bool IsSupported(Version cssVersion)
        {
            return true;
        }
    }
}
