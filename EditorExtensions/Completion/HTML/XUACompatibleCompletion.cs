using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class XUACompatibleCompletion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }
        
        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("http-equiv");

            if (attr != null && attr.Value.Equals("X-UA-Compatible", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("IE=edge", "Comma separate multiple value if needed"));
                result.Add(new SimpleHtmlCompletion("IE=7", "Comma separate multiple value if needed"));
                result.Add(new SimpleHtmlCompletion("IE=8", "Comma separate multiple value if needed"));
                result.Add(new SimpleHtmlCompletion("IE=9", "Comma separate multiple value if needed"));
                result.Add(new SimpleHtmlCompletion("FF=3", "Comma separate multiple value if needed"));
            }

            return result;
        }
    }
}
