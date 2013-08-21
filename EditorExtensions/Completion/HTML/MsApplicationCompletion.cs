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
    public class MsApplicationCompletion : IHtmlCompletionListProvider, IHtmlTreeVisitor
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        private List<string> _booleanNames = new List<string>()
        {
            "msapplication-allowdomainapicalls",
            "msapplication-allowdomainmetatags",
        };

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("name");

            if (attr == null)
                return result;

            if (_booleanNames.Contains(attr.Value.ToLowerInvariant()))
            {
                result.Add(new SimpleHtmlCompletion("false"));
                result.Add(new SimpleHtmlCompletion("true"));
            }
            else if (attr.Value.Equals("msapplication-window", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("width=1024;height=768"));
            }
            else if (attr.Value.Equals("msapplication-starturl", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("/"));
                result.Add(new SimpleHtmlCompletion("./index.html"));
                result.Add(new SimpleHtmlCompletion("/home/"));
                result.Add(new SimpleHtmlCompletion("http://example.com"));
            }
            else if (attr.Value.Equals("application-name", StringComparison.OrdinalIgnoreCase) ||
                     attr.Value.Equals("msapplication-tooltip", StringComparison.OrdinalIgnoreCase))
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
