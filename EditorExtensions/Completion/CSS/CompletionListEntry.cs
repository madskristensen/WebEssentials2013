using System;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class CompletionListEntry : ICssCompletionListEntry
    {
        private string _name;
        private StandardGlyphGroup _glyph;

        public CompletionListEntry(string name, int sortingPriority = 0, StandardGlyphGroup glyph = StandardGlyphGroup.GlyphGroupEnumMember)
        {
            _name = name;
            _glyph = glyph;
            SortingPriority = sortingPriority;
        }

        public string Description { get; set; }

        public string DisplayText
        {
            get { return _name; }
        }

        public string GetSyntax(Version version)
        {
            return string.Empty;
        }

        public StandardGlyphGroup StandardGlyph
        {
            get { return _glyph; }
        }

        public string GetAttribute(string name)
        {
            return string.Empty;
        }

        public string GetInsertionText(CssTextSource textSource, ITrackingSpan typingSpan)
        {
            return DisplayText;
        }

        public string GetVersionedAttribute(string name, Version version)
        {
            return GetAttribute(name);
        }

        public bool AllowQuotedString
        {
            get { return false; }
        }

        public bool IsBuilder
        {
            get { return false; }
        }

        public int SortingPriority { get; set; }


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
