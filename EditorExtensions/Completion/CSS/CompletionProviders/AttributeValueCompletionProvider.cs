using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.Html.Schemas;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("AttributeValueCompletionProvider")]
    internal class AttributeValueCompletionProvider : ICssCompletionListProvider
    {
        public const CssCompletionContextType AttributeValue = (CssCompletionContextType)1337;

        public CssCompletionContextType ContextType
        {
            get { return AttributeValue; }
        }
        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            HtmlSchemaManager mng = new HtmlSchemaManager();
            IHtmlSchema schema = mng.GetSchema("http://schemas.microsoft.com/intellisense/html");

            var tag = context.ContextItem.FindType<SimpleSelector>();
            var attr = context.ContextItem as AttributeSelector;

            if (tag != null && tag.Name != null && attr != null && attr.AttributeName != null)
            {
                return KnownTagName(schema, tag.Name.Text, attr.AttributeName.Text);
            }
            else if (attr != null && attr.AttributeName != null)
            {
                return UnknownTagName(schema, attr.AttributeName.Text);
            }

            return new List<ICssCompletionListEntry>();
        }

        private static IEnumerable<ICssCompletionListEntry> KnownTagName(IHtmlSchema schema, string tagName, string attrName)
        {
            var element = schema.GetElementInfo(tagName);

            if (element != null)
            {
                var attr = element.GetAttribute(attrName);

                if (attr == null)
                    yield break;

                foreach (var value in attr.GetValues())
                {
                    yield return new CompletionListEntry(value.Value);
                }
            }
        }

        private static IEnumerable<ICssCompletionListEntry> UnknownTagName(IHtmlSchema schema, string attrName)
        {
            var cache = new HashSet<string>();

            foreach (var element in schema.GetTopLevelElements())
            {
                var attr = element.GetAttribute(attrName);

                if (attr != null)
                {
                    foreach (var value in attr.GetValues())
                    {
                        cache.Add(value.Value);
                    }
                }
            }

            return cache.Select(n => new CompletionListEntry(n));
        }
    }
}
