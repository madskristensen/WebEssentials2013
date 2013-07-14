using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class TwitterCardCompletion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }
        
        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("name");

            if (attr == null)
                return result;

            if (attr.Value.Equals("twitter:card", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("app"));
                result.Add(new SimpleHtmlCompletion("gallery"));
                result.Add(new SimpleHtmlCompletion("photo"));
                result.Add(new SimpleHtmlCompletion("player"));
                result.Add(new SimpleHtmlCompletion("product"));
                result.Add(new SimpleHtmlCompletion("summary"));
                result.Add(new SimpleHtmlCompletion("summary_large-image"));
            }

            return result;
        }
    }
}
