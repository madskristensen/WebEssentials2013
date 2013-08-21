using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class OpenGraphTypeCompletion : IHtmlCompletionListProvider, IHtmlTreeVisitor
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("property");

            if (attr == null)
                return result;

            if (attr.Value.Equals("og:type", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("article"));
                result.Add(new SimpleHtmlCompletion("book"));
                result.Add(new SimpleHtmlCompletion("music"));
                result.Add(new SimpleHtmlCompletion("music.album"));
                result.Add(new SimpleHtmlCompletion("music.playlist"));
                result.Add(new SimpleHtmlCompletion("music.radio_station"));
                result.Add(new SimpleHtmlCompletion("music.song"));
                result.Add(new SimpleHtmlCompletion("profile"));
                result.Add(new SimpleHtmlCompletion("video"));
                result.Add(new SimpleHtmlCompletion("video.episode"));
                result.Add(new SimpleHtmlCompletion("video.movie"));
                result.Add(new SimpleHtmlCompletion("video.movie"));
                result.Add(new SimpleHtmlCompletion("video.other"));
                result.Add(new SimpleHtmlCompletion("video.tv_show"));
                result.Add(new SimpleHtmlCompletion("website"));
            }
            if (attr.Value.Equals("og:video:type", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string value in MetaHttpEquivCompletion.GetAttributeValue("source", "type"))
                {
                    if (value.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                        result.Add(new SimpleHtmlCompletion(value));
                }

                result.Add(new SimpleHtmlCompletion("application/x-shockwave-flash"));
            }
            if (attr.Value.Equals("og:audio:type", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string value in MetaHttpEquivCompletion.GetAttributeValue("source", "type"))
                {
                    if (value.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                        result.Add(new SimpleHtmlCompletion(value));
                }
            }
            else if (attr.Value.Equals("og:site_name", StringComparison.OrdinalIgnoreCase) ||
                     attr.Value.Equals("og:title", StringComparison.OrdinalIgnoreCase))
            {
                if (context.Element.Parent == null)
                    return result;

                var list = new List<string>();

                context.Element.Parent.Accept(this, list);

                foreach (var item in list)
                {
                    result.Add(new SimpleHtmlCompletion(item));
                }
            }

            return result;
        }

        public bool Visit(ElementNode element, object parameter)
        {
            if (element.Name.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                var list = (List<string>)parameter;

                string text = element.GetText(element.InnerRange);
                if (!list.Contains(text))
                    list.Add(text);
            }

            return true;
        }
    }
}
