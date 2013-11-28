using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class OpenGraphTypeCompletion : StaticListCompletion, IHtmlTreeVisitor
    {
        protected override string KeyProperty { get { return "property"; } }
        public OpenGraphTypeCompletion()
            : base(new Dictionary<string, IList<HtmlCompletion>>(StringComparer.OrdinalIgnoreCase)
            {
                { "og:type",            Values("article", "book", "music", "music.album", "music.playlist", 
                                               "music.radio_station", "music.song", "profile", "video",
                                               "video.episode", "video.movie", "video.movie", "video.other", 
                                               "video.tv_show", "website") },
                { "og:audio:type",      Values(MetaHttpEquivCompletion.GetAttributeValue("source", "type").Where(v => v.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))) },
                { "og:video:type",      Values(MetaHttpEquivCompletion.GetAttributeValue("source", "type").Where(v => v.StartsWith("video/", StringComparison.OrdinalIgnoreCase)).Concat(new[]{"application/x-shockwave-flash"})) },
                { "apple-mobile-web-app-status-bar-style",  Values("default", "black", "black-translucent") }
            }) { }

        public new IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var attr = context.Element.GetAttribute("property");

            if (attr == null)
                return Empty;

            if (attr.Value.Equals("og:site_name", StringComparison.OrdinalIgnoreCase)
             || attr.Value.Equals("og:title", StringComparison.OrdinalIgnoreCase))
            {
                if (context.Element.Parent == null)
                    return Empty;

                var list = new HashSet<string>();
                context.Element.Parent.Accept(this, list);
                return Values(list);
            }

            return base.GetEntries(context);
        }

        public bool Visit(ElementNode element, object parameter)
        {
            if (element.Name.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                var list = (HashSet<string>)parameter;

                string text = element.GetText(element.InnerRange);
                list.Add(text);
            }

            return true;
        }
    }
}
