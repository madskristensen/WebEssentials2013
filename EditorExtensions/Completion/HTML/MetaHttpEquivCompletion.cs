using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.Html.Schemas;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class MetaHttpEquivCompletion : StaticListCompletion
    {
        protected override string KeyProperty { get { return "http-equiv"; } }
        public MetaHttpEquivCompletion()
            : base(new Dictionary<string, IList<HtmlCompletion>>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-UA-Compatible",    Values("IE=edge", "IE=7", "IE=8", "IE=9", "FF=3").WithDescription("Separate multiple values with commas if needed") },
                { "Content-Type",       Values(GetAttributeValue("meta", "charset").Select(c => "text/html; charset=" + c)) },
                { "refresh",            Values("3", "3; url=http://example.com") },
                { "Content-Language",       Values(GetAttributeValue("html", "lang")) },
                { "Set-Cookie",         Values("name=value; expires=Fri, 30 Dec 2019 12:00:00 GMT; path=/", "name=value; expires=Fri, 30 Dec 2019 12:00:00 GMT; path=http://example.com") },
            }) { }

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
