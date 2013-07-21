using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class OpenGraphTypeCompletion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }
        
        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("property");

            if (attr != null && attr.Value.Equals("og:type", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("article"));
                result.Add(new SimpleHtmlCompletion("book"));
                result.Add(new SimpleHtmlCompletion("music.album"));
                result.Add(new SimpleHtmlCompletion("music.playlist"));
                result.Add(new SimpleHtmlCompletion("music.radio_station"));
                result.Add(new SimpleHtmlCompletion("music.song"));
                result.Add(new SimpleHtmlCompletion("profile"));
                result.Add(new SimpleHtmlCompletion("video.episode"));
                result.Add(new SimpleHtmlCompletion("video.movie"));
                result.Add(new SimpleHtmlCompletion("video.movie"));
                result.Add(new SimpleHtmlCompletion("video.other"));
                result.Add(new SimpleHtmlCompletion("video.tv_show"));    
                result.Add(new SimpleHtmlCompletion("website"));

            }

            return result;
        }
    }
}
