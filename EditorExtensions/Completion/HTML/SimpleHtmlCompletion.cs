using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor;
using System.Windows.Media;

namespace MadsKristensen.EditorExtensions
{
    public class SimpleHtmlCompletion : HtmlCompletion
    {
        private static ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);

        public SimpleHtmlCompletion(string value)
            : base(value, value, null, _glyph, HtmlIconAutomationText.AttributeIconText)
        { }
    }
}
