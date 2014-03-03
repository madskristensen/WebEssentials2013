using System;
using System.Collections.Generic;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [HtmlCompletionProvider(CompletionType.Values, "link", "sizes")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AppleLinkCompletion : StaticListCompletion
    {
        protected override string KeyProperty { get { return "rel"; } }
        public AppleLinkCompletion()
            : base(new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "apple-touch-icon", Values("72x72", "114x114", "144x144") }
            }) { }
    }
}
