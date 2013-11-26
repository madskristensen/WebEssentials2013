using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

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
            var list = new HashSet<string>();

            context.Document.HtmlEditorTree.RootNode.Accept(this, list);

            return list.Select(s => new SimpleHtmlCompletion(s)).ToList<HtmlCompletion>();
        }

        public bool Visit(ElementNode element, object parameter)
        {
            if (element.Name.Equals("datalist", StringComparison.OrdinalIgnoreCase))
            {
                var list = (HashSet<string>)parameter;
                list.Add(element.Id);
            }

            return true;
        }
    }
}
