using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("VariableNameCompletionProvider")]
    internal class VariableNameCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)609; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            var visitor = new CssItemCollector<Declaration>();
            context.ContextItem.StyleSheet.Accept(visitor);

            foreach (Declaration dec in visitor.Items)
            {
                if (dec.IsValid && dec.PropertyName.Text.StartsWith("var-"))
                    yield return new CompletionListEntry(dec.PropertyName.Text.Substring(4));
            }
        }
    }
}
