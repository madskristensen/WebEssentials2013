using System.Collections.Generic;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

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
            var list = new HashSet<string>();
            var glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);

            context.Document.HtmlEditorTree.RootNode.Accept(this, list);

            return list.Select(s => new HtmlCompletion(s, s, s, glyph, HtmlIconAutomationText.AttributeIconText)).ToList();
        }

        public bool Visit(ElementNode element, object parameter)
        {
            if (_inputTypes.Contains(element.Name.ToLowerInvariant()))
            {
                var list = (HashSet<string>)parameter;
                var id = element.GetAttribute("id");

                if (id != null)
                    list.Add(id.Value);
            }

            return true;
        }
    }
}
