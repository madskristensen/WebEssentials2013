using Microsoft.Html.Editor.Intellisense;
using Microsoft.Html.Schemas;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class MetaHttpEquivCompletion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("http-equiv");

            if (attr == null)
                return result;

            if (attr.Value.Equals("X-UA-Compatible", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("IE=edge", "Comma separate multiple value if needed"));
                result.Add(new SimpleHtmlCompletion("IE=7", "Comma separate multiple value if needed"));
                result.Add(new SimpleHtmlCompletion("IE=8", "Comma separate multiple value if needed"));
                result.Add(new SimpleHtmlCompletion("IE=9", "Comma separate multiple value if needed"));
                result.Add(new SimpleHtmlCompletion("FF=3", "Comma separate multiple value if needed"));
            }
            else if (attr.Value.Equals("content-type", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string value in GetAttributeValue("meta", "charset"))
                {
                    result.Add(new SimpleHtmlCompletion("text/html; charset=" + value));
                }
            }
            else if (attr.Value.Equals("refresh", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("3"));
                result.Add(new SimpleHtmlCompletion("3; url=http://example.com"));
            }
            else if (attr.Value.Equals("content-language", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string value in GetAttributeValue("html", "lang"))
                {
                    result.Add(new SimpleHtmlCompletion(value));
                }
            }
            else if (attr.Value.Equals("set-cookie", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SimpleHtmlCompletion("name=value; expires=Fri, 30 Dec 2019 12:00:00 GMT; path=/"));
                result.Add(new SimpleHtmlCompletion("name=value; expires=Fri, 30 Dec 2019 12:00:00 GMT; path=http://example.com"));
            }

            return result;
        }

        public static IEnumerable<string> GetAttributeValue(string elementName, string attributeName)
        {
            HtmlSchemaManager mng = new HtmlSchemaManager();
            IHtmlSchema schema = mng.GetSchema("http://schemas.microsoft.com/intellisense/html");
            IHtmlElementInfo element = schema.GetElementInfo(elementName);

            if (element != null)
            {
                IHtmlAttributeInfo attribute = element.GetAttribute(attributeName);

                if (attribute != null)
                {
                    foreach (IHtmlAttributeValueInfo value in attribute.GetValues())
                    {
                        yield return value.Value;
                    }
                }
            }
        }
    }
}
