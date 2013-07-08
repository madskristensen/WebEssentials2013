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
    [Name("ClassCompletionProvider")]
    internal class ClassCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)602; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            HashSet<string> classNames = new HashSet<string>();
            HashSet<ICssCompletionListEntry> entries = new HashSet<ICssCompletionListEntry>();
            
            StyleSheet stylesheet = context.ContextItem.StyleSheet;
            var visitorRules = new CssItemCollector<ClassSelector>();
            stylesheet.Accept(visitorRules);

            foreach (ClassSelector item in visitorRules.Items)
            {
                if (item != context.ContextItem && !classNames.Contains(item.Text))
                {
                    classNames.Add(item.Text);
                    entries.Add(new CompletionListEntry(item.Text));
                }
            }

            return entries;
        }

    }
}
