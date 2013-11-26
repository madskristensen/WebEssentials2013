using System.Windows.Media;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    public class SimpleHtmlCompletion : HtmlCompletion
    {
        private static ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);

        public SimpleHtmlCompletion(string displayText)
            : base(displayText, displayText, null, _glyph, HtmlIconAutomationText.AttributeIconText)
        { }

        public SimpleHtmlCompletion(string displayText, string description)
            : base(displayText, displayText, description, _glyph, HtmlIconAutomationText.AttributeIconText)
        { }
    }
}
