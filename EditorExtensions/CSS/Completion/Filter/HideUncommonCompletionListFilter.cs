using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssCompletionListFilter))]
    [Name("HideUncommonCompletionListFilter")]
    internal class HideUncommonCompletionListFilter : ICssCompletionListFilter
    {
        private static readonly StringCollection _cache = new StringCollection() 
        { 
            "widows", // Rarely used and get's in the way of "width"
        };

        public void FilterCompletionList(IList<CssCompletionEntry> completions, CssCompletionContext context)
        {
            if (context.ContextType != CssCompletionContextType.PropertyName)
                return;

            foreach (CssCompletionEntry entry in completions)
            {
                if (_cache.Contains(entry.DisplayText))
                {
                    entry.FilterType = CompletionEntryFilterTypes.NeverVisible;
                }
            }
        }
    }
}