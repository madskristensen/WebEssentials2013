using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("ImportantCompletionProvider")]
    internal class ImportantCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return CssCompletionContextType.PropertyValue; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            List<ICssCompletionListEntry> entries = new List<ICssCompletionListEntry>();
            Declaration dec = context.ContextItem.FindType<Declaration>();
            if (dec == null || dec.Colon == null || dec.Important != null || dec.Values.Count == 0)
                return entries;

            ParseItem before = dec.ItemBeforePosition(context.SpanStart);
            if (before != null && before.Text == "!")
                entries.Add(new CompletionListEntry("important", 1));

            return entries;
        }
    }
}
