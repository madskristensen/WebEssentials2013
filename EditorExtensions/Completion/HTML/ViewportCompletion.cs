using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class ViewportCompletion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }
        
        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("name");

            if (attr != null && attr.Value.Equals("viewport", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("width=device-width, initial-scale=1.0", "Example viewport value"));
                result.Add(new SimpleHtmlCompletion("width=device-width, initial-scale=1.0, user-scalable=no", "Example viewport value"));
                result.Add(new SimpleHtmlCompletion("width=device-width, initial-scale=1.0, maximum-scale=1", "Example viewport value"));
                result.Add(new SimpleHtmlCompletion("width=device-width, initial-scale=1.0, minimum-scale=1", "Example viewport value"));
            }

            return result;
        }
    }
}
