using System;
using System.Linq;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CSS.Editor.Intellisense;
using System.Collections.Generic;

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
            StyleSheet stylesheet = context.ContextItem.StyleSheet;
            var visitorRules = new CssItemCollector<ClassSelector>();
            stylesheet.Accept(visitorRules);

            foreach (string item in visitorRules.Items.Where(s => s != context.ContextItem).Select(s=> s.Text).Distinct())
            {
                yield return new CompletionListEntry(item);
            }
        }
    }
}
