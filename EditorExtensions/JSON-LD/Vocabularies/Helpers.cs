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
            Type = type;
        }

        public string Name { get; private set; }
        public PropertyType Type { get; private set; }

        public ImageSource GetGlyph()
        {
            if (Type == PropertyType.Object)
                return GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupNamespace, StandardGlyphItem.GlyphItemPublic);

            if (Type == PropertyType.Array)
                return GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupType, StandardGlyphItem.TotalGlyphItems);

            return GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
        }
    }

    public enum PropertyType
    {
        String,
        Object,
        Array,
    }
}
