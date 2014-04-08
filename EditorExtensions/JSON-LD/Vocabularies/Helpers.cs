using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.JSONLD
{
    public class Entry
    {
        public Entry(string name, PropertyType type = PropertyType.String)
        {
            Name = name;
            EntryType = type;
        }

        public string Name { get; private set; }
        public PropertyType EntryType { get; private set; }

        public ImageSource Glyph
        {
            get
            {
                if (EntryType == PropertyType.Object)
                    return GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupNamespace, StandardGlyphItem.GlyphItemPublic);

                if (EntryType == PropertyType.Array)
                    return GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupType, StandardGlyphItem.TotalGlyphItems);

                return GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
            }
        }
    }

    public enum PropertyType
    {
        String,
        Object,
        Array,
    }
}
