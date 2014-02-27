using System;
using System.Windows.Media;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions.Css
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

        public ITrackingSpan ApplicableTo
        {
            get { return null; }
        }

        public CompletionEntryFilterTypes FilterType
        {
            get { return CompletionEntryFilterTypes.MatchTyping; }
        }

        public ImageSource Icon
        {
            get { return GlyphService.GetGlyph(_glyph, StandardGlyphItem.GlyphItemPublic); }
        }

        public bool IsCommitChar(char typedCharacter)
        {
            return false;
        }

        public bool IsMuteCharacter(char typedCharacter)
        {
            return false;
        }

        public bool RetriggerIntellisense
        {
            get { return false; }
        }
    }
}
