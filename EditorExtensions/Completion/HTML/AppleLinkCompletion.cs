using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "link", "sizes")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AppleLinkCompletion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }
        
        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("rel");
            
            if (attr == null)
                return result;

            if (attr.Value.Equals("apple-touch-icon", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("72x72"));
                result.Add(new SimpleHtmlCompletion("114x114"));
                result.Add(new SimpleHtmlCompletion("144x144"));
            }

            return result;
        }
    }
}
