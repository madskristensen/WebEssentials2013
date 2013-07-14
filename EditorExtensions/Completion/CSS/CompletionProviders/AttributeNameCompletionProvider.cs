using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.Html.Schemas;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("AttributeNameCompletionProvider")]
    internal class AttributeNameCompletionProvider : ICssCompletionListProvider
    {
        private static HashSet<string> _allAttributes;

        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)605; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            HtmlSchemaManager mng = new HtmlSchemaManager();
            IHtmlSchema schema = mng.GetSchema("http://schemas.microsoft.com/intellisense/html");

            var tag = context.ContextItem.FindType<SimpleSelector>();

            if (tag != null && tag.Name != null)
            {
                return KnownTagName(schema, tag.Name.Text);
            }

            return UnknownTagName(schema);
        }

        private IEnumerable<ICssCompletionListEntry> KnownTagName(IHtmlSchema schema, string tagName)
        {
            var element = schema.GetElementInfo(tagName);

            if (element != null)
            {
                foreach (var attr in element.GetAttributes())
                {
                    if (IsAllowed(attr.Name))
                        yield return new CompletionListEntry(attr.Name);
                }
            }
        }

        private IEnumerable<ICssCompletionListEntry> UnknownTagName(IHtmlSchema schema)
        {
            if (_allAttributes == null)
            {
                _allAttributes = new HashSet<string>();

                foreach (var element in schema.GetTopLevelElements())
                    foreach (var attr in element.GetAttributes())
                    {
                        if (!_allAttributes.Contains(attr.Name) && IsAllowed(attr.Name))
                            _allAttributes.Add(attr.Name);
                    }
            }

            return _allAttributes.Select(n => new CompletionListEntry(n));
        }

        private static bool IsAllowed(string name)
        {
            return !name.Any(c => char.IsUpper(c)) && // WebForms server attributes
                   !name.StartsWith("on", StringComparison.OrdinalIgnoreCase) && // Client- and server-side event handlers
                   !name.Equals("runat", StringComparison.OrdinalIgnoreCase);
        }
    }
}
