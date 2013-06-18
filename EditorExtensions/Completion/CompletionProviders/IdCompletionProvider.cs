using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("IdCompletionProvider")]
    internal class IdCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)603; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            List<string> idNames = new List<string>();
            List<ICssCompletionListEntry> entries = new List<ICssCompletionListEntry>();

            StyleSheet stylesheet = context.ContextItem.StyleSheet;
            var visitorRules = new CssItemCollector<IdSelector>();
            stylesheet.Accept(visitorRules);

            foreach (IdSelector item in visitorRules.Items)
            {
                if (item != context.ContextItem && !idNames.Contains(item.Text))
                {
                    idNames.Add(item.Text);
                    entries.Add(new CompletionListEntry(item.Text));
                }
            }

            return entries;
        }

    }
}
