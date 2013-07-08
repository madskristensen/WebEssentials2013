using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "input", "list")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class HtmlDataListCompletion : IHtmlCompletionListProvider, IHtmlTreeVisitor
    {
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
            if (element.Name.Equals("datalist", StringComparison.OrdinalIgnoreCase))
            {
                var list = (List<string>)parameter;

                if (!list.Contains(element.Id))
                    list.Add(element.Id);
            }

            return true;
        }
    }
}
