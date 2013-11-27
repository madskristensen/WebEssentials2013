using System;
using System.Collections.Generic;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class MSApplicationCompletion : StaticListCompletion, IHtmlTreeVisitor
    {
        private static readonly IList<HtmlCompletion> BooleanValues = Values("false", "true");

        protected override string KeyProperty { get { return "name"; } }
        public MSApplicationCompletion()
            : base(new Dictionary<string, IList<HtmlCompletion>>(StringComparer.OrdinalIgnoreCase)
            {
                { "MSApplication-AllowDomainApiCalls",  BooleanValues },
                { "MSApplication-AllowDomainMetaTags",  BooleanValues },
                { "MSApplication-Window",               Values("width=1024;height=768") },
                { "MSApplication-StartURL",             Values("/", "./index.html", "/home/", "http://example.com") }
            }) { }


        public new IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
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
