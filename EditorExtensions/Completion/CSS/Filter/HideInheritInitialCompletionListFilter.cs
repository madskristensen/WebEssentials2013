using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionListFilter))]
    [Name("Inherit/Initial Filter")]
    internal class HideInheritInitialCompletionListFilter : ICssCompletionListFilter
    {
        public void FilterCompletionList(IList<CssCompletionEntry> completions, CssCompletionContext context)
        {
            if (context.ContextType != CssCompletionContextType.PropertyValue || WESettings.Instance.Css.ShowInitialInherit)
                return;

            foreach (CssCompletionEntry entry in completions)
            {
                if (entry.DisplayText == "initial" || entry.DisplayText == "inherit")
                {
                    entry.FilterType = CompletionEntryFilterTypes.NeverVisible;
                }
            }
        }
    }
}