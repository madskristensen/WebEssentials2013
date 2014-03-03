using System;
using System.Collections.Generic;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class MiscMetaCompletion : StaticListCompletion
    {
        protected override string KeyProperty { get { return "name"; } }
        public MiscMetaCompletion()
            : base(new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "generator",  Values("Visual Studio") },
                { "robots",     Values("index", "noindex", "follow", "nofollow", "noindex, nofollow", "noindex, follow", "index, nofollow") }
          }) { }
    }
}
