using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "*", "id")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class InputIdAttributeCompletion : IHtmlCompletionListProvider, IHtmlTreeVisitor
    {
        private static readonly HashSet<string> _inputTypes = new HashSet<string>() { "input", "textarea", "select" };
        private static ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);

        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var list = new HashSet<string>();
            string tooltip = string.Empty;

            if (context.Element != null && _inputTypes.Contains(context.Element.Name))
            {
                context.Document.HtmlEditorTree.RootNode.Accept(this, list);
                tooltip = "Extracted from a <label> element in this document";
            }

            return list.Select(s => new HtmlCompletion(s, s, tooltip, _glyph, HtmlIconAutomationText.AttributeIconText)).ToList();
        }

        public bool Visit(ElementNode element, object parameter)
        {
            if (element.Name == "label")
            {
                var list = (HashSet<string>)parameter;
                var forAttr = element.GetAttribute("for");

                if (forAttr != null)
                    list.Add(forAttr.Value);
            }

            return true;
        }
    }
}
