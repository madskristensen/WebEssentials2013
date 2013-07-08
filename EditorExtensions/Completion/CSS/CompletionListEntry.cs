using System;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    internal class CompletionListEntry : ICssCompletionListEntry
    {
        private string _name;

        public CompletionListEntry(string name, int sortingPriority = 0)
        {
            _name = name;
            SortingPriority = sortingPriority;
        }

        public string Description
        {
            get { return string.Empty; }
        }

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
            get { return StandardGlyphGroup.GlyphGroupEnumMember; }
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
