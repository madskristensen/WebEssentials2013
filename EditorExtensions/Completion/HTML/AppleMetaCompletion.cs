using System;
using System.Collections.Generic;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AppleMetaCompletion : StaticListCompletion
    {
        protected override string KeyProperty { get { return "name"; } }
        public AppleMetaCompletion()
            : base(new Dictionary<string, IList<HtmlCompletion>>(StringComparer.OrdinalIgnoreCase)
            {
                { "apple-mobile-web-app-capable",           Values("yes", "no") },
                { "format-detection",                       Values("telephone=yes", "telephone=no") },
                { "apple-mobile-web-app-status-bar-style",  Values("default", "black", "black-translucent") }
            }) { }
    }
}
