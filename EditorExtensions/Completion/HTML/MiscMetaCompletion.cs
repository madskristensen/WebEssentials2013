using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class MiscMetaCompletion : IHtmlCompletionListProvider
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

            if (attr.Value.Equals("generator", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("Visual Studio"));
            }
            if (attr.Value.Equals("robots", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("index"));
                result.Add(new SimpleHtmlCompletion("noindex"));
                result.Add(new SimpleHtmlCompletion("follow"));
                result.Add(new SimpleHtmlCompletion("nofollow"));
                result.Add(new SimpleHtmlCompletion("noindex, nofollow"));
                result.Add(new SimpleHtmlCompletion("noindex, follow"));
                result.Add(new SimpleHtmlCompletion("index, nofollow"));
            }

            return result;
        }
    }
}
