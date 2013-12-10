using System;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class RegionCompletionListEntry : ICssCompletionListEntry
    {
        public string Description
        {
            get { return string.Empty; }
        }

        public string DisplayText
        {
            get { return "Add region..."; }
        }

        public string GetSyntax(Version version)
        {
            return string.Empty;
        }

        public StandardGlyphGroup StandardGlyph
        {
            get { return StandardGlyphGroup.GlyphCSharpExpansion; }
        }

        public string GetAttribute(string name)
        {
            return string.Empty;
        }

        public string GetInsertionText(CssTextSource textSource, ITrackingSpan typingSpan)
        {
            return "region";//"/*#region MyRegion */\n\n\n\n/*#endregion*/";
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
            get { return true; }
        }

        public int SortingPriority { get { return 0; } }


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
