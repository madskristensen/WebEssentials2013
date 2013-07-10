using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.Completion.CompletionProviders
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("InputTypeCompletionProvider")]
    internal class InputTypeCompletionProvider : ICssCompletionListProvider
    {
        public const CssCompletionContextType ContextTypeValue = (CssCompletionContextType)1337;

        public CssCompletionContextType ContextType
        {
            get { return ContextTypeValue; }
        }
        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            // list taken from http://dev.w3.org/html5/markup/input.html
            return new[] { 
                new CompletionListEntry("text"),
                new CompletionListEntry("password"),
                new CompletionListEntry("checkbox"),
                new CompletionListEntry("radio"),
                new CompletionListEntry("button"),
                new CompletionListEntry("submit"),
                new CompletionListEntry("reset"),
                new CompletionListEntry("file"),
                new CompletionListEntry("hidden"),
                new CompletionListEntry("image"),
                new CompletionListEntry("datetime"),
                new CompletionListEntry("datetime-local"),
                new CompletionListEntry("date"),
                new CompletionListEntry("month"),
                new CompletionListEntry("time"),
                new CompletionListEntry("week"),
                new CompletionListEntry("number"),
                new CompletionListEntry("range"),
                new CompletionListEntry("email"),
                new CompletionListEntry("url"),
                new CompletionListEntry("search"),
                new CompletionListEntry("tel"),
                new CompletionListEntry("color")
            };

        }
    }
}
