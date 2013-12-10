using System;
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
            var tag = context.ContextItem.FindType<SimpleSelector>();
            var schemas = GetSchemas();

            if (tag == null || tag.Name == null)
            {
                return UnknownTagName(schemas);
            }

            return KnownTagName(schemas, tag.Name.Text);
        }

        public static List<IHtmlSchema> GetSchemas()
        {
            HtmlSchemaManager mng = new HtmlSchemaManager();
            IHtmlSchema html = mng.GetSchema("http://schemas.microsoft.com/intellisense/html");

            var schemas = mng.CustomAttributePrefixes.SelectMany(p => mng.GetSupplementalSchemas(p)).ToList();
            schemas.Insert(0, html);

            return schemas;
        }

        private static IEnumerable<ICssCompletionListEntry> KnownTagName(IEnumerable<IHtmlSchema> schemas, string tagName)
        {
            foreach (IHtmlSchema schema in schemas)
            {
                var element = schema.GetElementInfo(tagName) ?? schema.GetElementInfo("___all___");

                if (element != null)
                {
                    foreach (var attr in element.GetAttributes())
                    {
                        if (IsAllowed(attr.Name))
                            yield return new CompletionListEntry(attr.Name);
                    }
                }
            }
        }

        private static IEnumerable<ICssCompletionListEntry> UnknownTagName(IEnumerable<IHtmlSchema> schemas)
        {
            if (_allAttributes == null)
            {
                _allAttributes = new HashSet<string>();

                foreach (var schema in schemas)
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
