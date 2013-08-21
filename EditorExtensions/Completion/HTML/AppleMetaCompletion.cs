using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AppleMetaCompletion : IHtmlCompletionListProvider
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

            if (attr.Value.Equals("apple-mobile-web-app-capable", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("yes"));
                result.Add(new SimpleHtmlCompletion("no"));
            }
            else if (attr.Value.Equals("format-detection", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("telephone=yes"));
                result.Add(new SimpleHtmlCompletion("telephone=no"));
            }
            else if (attr.Value.Equals("apple-mobile-web-app-status-bar-style", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("default"));
                result.Add(new SimpleHtmlCompletion("black"));
                result.Add(new SimpleHtmlCompletion("black-translucent"));
            }

            return result;
        }
    }
}
