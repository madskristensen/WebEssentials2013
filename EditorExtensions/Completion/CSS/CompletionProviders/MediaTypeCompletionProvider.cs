using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("MediaTypeCompletionProvider")]
    internal class MediaTypeCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)612; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            yield return new CompletionListEntry("all");
            yield return new CompletionListEntry("aural");
            yield return new CompletionListEntry("braille");
            yield return new CompletionListEntry("embossed");
            yield return new CompletionListEntry("handheld");            
            yield return new CompletionListEntry("print");
            yield return new CompletionListEntry("projection");
            yield return new CompletionListEntry("screen");
            yield return new CompletionListEntry("tty");
            yield return new CompletionListEntry("tv");
                        
            MediaQuery query = (MediaQuery)context.ContextItem;
            ParseItem item = query.StyleSheet.ItemAfterPosition(context.SpanStart);

            if (item != query.MediaType || query.Operation == null)
            {
                yield return new CompletionListEntry("not");
                yield return new CompletionListEntry("only");
            }
        }
    }
}
