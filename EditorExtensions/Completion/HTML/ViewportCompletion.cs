using System;
using System.Collections.Generic;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class ViewportCompletion : StaticListCompletion
    {
        protected override string KeyProperty { get { return "name"; } }
        public ViewportCompletion()
            : base(new Dictionary<string, IList<HtmlCompletion>>(StringComparer.OrdinalIgnoreCase)
            {
                { "viewport", Values(
                    "width=device-width, initial-scale=1.0", 
                    "width=device-width, initial-scale=1.0, user-scalable=no", 
                    "width=device-width, initial-scale=1.0, maximum-scale=1", 
                    "width=device-width, initial-scale=1.0, minimum-scale=1"
                ).WithDescription("Sample viewport value") }
            }) { }
    }
}
