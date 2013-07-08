using System;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    internal class UrlPickerCompletionListEntry : ICssCompletionListEntry
    {
        private string _name;

        public UrlPickerCompletionListEntry(string name)
        {
            _name = name;
        }

        public bool AllowQuotedString
        {
            get { return false; }
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

        public int SortingPriority
        {
            get { return IsFolder ? 1 : 0; }
        }

        public bool IsBuilder
        {
            get { return false; }
        }

        public StandardGlyphGroup StandardGlyph
        {
            get { return IsFolder ? StandardGlyphGroup.GlyphClosedFolder : StandardGlyphGroup.GlyphBscFile; }
        }

        public string GetAttribute(string name)
        {
            return string.Empty;
        }

        public string GetInsertionText(CssTextSource textSource, ITrackingSpan typingSpan)
        {
            if (IsFolder)
            {
                return DisplayText + "/";
            }

            return DisplayText;
        }

        public string GetVersionedAttribute(string name, Version version)
        {
            return GetAttribute(name);
        }

        private bool IsFolder
        {
            get { return !DisplayText.Contains("."); }
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
