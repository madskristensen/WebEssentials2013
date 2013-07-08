using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "label", "for")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class HtmlLabelForAttributeCompletion : IHtmlCompletionListProvider, IHtmlTreeVisitor
    {
        private static readonly List<string> _inputTypes = new List<string>() { "input", "textarea", "select" };
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var list = new List<string>();
            var glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);

            context.Document.HtmlEditorTree.RootNode.Accept(this, list);

            foreach (var item in list)
            {
                var completion = new HtmlCompletion(item, item, item, glyph, HtmlIconAutomationText.AttributeIconText);
                result.Add(completion);
            }

            return result;
        }

        public bool Visit(ElementNode element, object parameter)
        {
            if (_inputTypes.Contains(element.Name.ToLowerInvariant()))
            {
                var list = (List<string>)parameter;
                var id = element.GetAttribute("id");

                if (id != null && !list.Contains(id.Value))
                    list.Add(id.Value);
            }

            return true;
        }
    }
}
