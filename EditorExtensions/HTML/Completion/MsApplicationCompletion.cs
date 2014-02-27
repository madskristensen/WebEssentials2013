using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class MSApplicationCompletion : StaticListCompletion, IHtmlTreeVisitor
    {
        private static readonly IEnumerable<string> BooleanValues = Values("false", "true");

        protected override string KeyProperty { get { return "name"; } }
        public MSApplicationCompletion()
            : base(new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "MSApplication-AllowDomainApiCalls",  BooleanValues },
                { "MSApplication-AllowDomainMetaTags",  BooleanValues },
                { "MSApplication-Window",               Values("width=1024;height=768") },
                { "MSApplication-StartURL",             Values("/", "./index.html", "/home/", "http://example.com") }
            }) { }


        public override IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var attr = context.Element.GetAttribute("name");

            if (attr == null)
                return Empty;

            if (attr.Value.Equals("application-name", StringComparison.OrdinalIgnoreCase)
             || attr.Value.Equals("msapplication-tooltip", StringComparison.OrdinalIgnoreCase))
            {
                if (context.Element.Parent == null)
                    return Empty;

                var list = new HashSet<string>();

                context.Element.Parent.Accept(this, list);
                return new List<HtmlCompletion>(list.Select(l => new SimpleHtmlCompletion(l, context.Session)));
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
