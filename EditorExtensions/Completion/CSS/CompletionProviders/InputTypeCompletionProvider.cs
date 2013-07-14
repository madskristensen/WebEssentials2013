using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.Completion.CompletionProviders
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("InputTypeCompletionProvider")]
    internal class InputTypeCompletionProvider : ICssCompletionListProvider
    {
        public const CssCompletionContextType InputTypeValue = (CssCompletionContextType)1337;

        public CssCompletionContextType ContextType
        {
            get { return InputTypeValue; }
        }
        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            // list taken from http://dev.w3.org/html5/markup/input.html
            yield return new CompletionListEntry("text");
            yield return new CompletionListEntry("password");
            yield return new CompletionListEntry("checkbox");
            yield return new CompletionListEntry("radio");
            yield return new CompletionListEntry("button");
            yield return new CompletionListEntry("submit");
            yield return new CompletionListEntry("reset");
            yield return new CompletionListEntry("file");
            yield return new CompletionListEntry("hidden");
            yield return new CompletionListEntry("image");
            yield return new CompletionListEntry("datetime");
            yield return new CompletionListEntry("datetime-local");
            yield return new CompletionListEntry("date");
            yield return new CompletionListEntry("month");
            yield return new CompletionListEntry("time");
            yield return new CompletionListEntry("week");
            yield return new CompletionListEntry("number");
            yield return new CompletionListEntry("range");
            yield return new CompletionListEntry("email");
            yield return new CompletionListEntry("url");
            yield return new CompletionListEntry("search");
            yield return new CompletionListEntry("tel");
            yield return new CompletionListEntry("color");
        }
    }
}
