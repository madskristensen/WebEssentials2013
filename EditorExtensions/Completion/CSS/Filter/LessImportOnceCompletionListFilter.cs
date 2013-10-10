using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionListFilter))]
    [Name("import-once Filter")]
    internal class LessImportOnceCompletionListFilter : ICssCompletionListFilter
    {
        public void FilterCompletionList(IList<CssCompletionEntry> completions, CssCompletionContext context)
        {
            if (context.ContextType != CssCompletionContextType.AtDirectiveName)
                return;

            var importOnce = completions.FirstOrDefault(c => c.DisplayText == "@import-once");
            if (importOnce != null)
                importOnce.FilterType = CompletionEntryFilterTypes.NeverVisible;
        }
    }
}