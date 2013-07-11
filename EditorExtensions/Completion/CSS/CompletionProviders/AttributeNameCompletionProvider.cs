using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("AttributeNamdCompletionProvider")]
    internal class AttributeNamdCompletionProvider : ICssCompletionListProvider
    {
        private static IEnumerable<ICssCompletionListEntry> _entryCache = GetListEntriesCache();

        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)605; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            return _entryCache;
        }

        private static IEnumerable<ICssCompletionListEntry> GetListEntriesCache()
        {
            List<ICssCompletionListEntry> entries = new List<ICssCompletionListEntry>();

            // Global
            entries.Add(new CompletionListEntry("accesskey"));
            entries.Add(new CompletionListEntry("class"));
            entries.Add(new CompletionListEntry("contenteditable"));
            entries.Add(new CompletionListEntry("contextmenu"));
            entries.Add(new CompletionListEntry("dir"));
            entries.Add(new CompletionListEntry("draggable"));
            entries.Add(new CompletionListEntry("dropzone"));
            entries.Add(new CompletionListEntry("hidden"));
            entries.Add(new CompletionListEntry("id"));
            entries.Add(new CompletionListEntry("lang"));
            entries.Add(new CompletionListEntry("spellcheck"));
            entries.Add(new CompletionListEntry("style"));
            entries.Add(new CompletionListEntry("tabindex"));
            entries.Add(new CompletionListEntry("title"));
            entries.Add(new CompletionListEntry("translate"));

            // Common
            entries.Add(new CompletionListEntry("alt"));
            entries.Add(new CompletionListEntry("height"));
            entries.Add(new CompletionListEntry("rel"));
            entries.Add(new CompletionListEntry("role"));
            entries.Add(new CompletionListEntry("width"));
            entries.Add(new CompletionListEntry("src"));

            // A element
            entries.Add(new CompletionListEntry("target"));
            entries.Add(new CompletionListEntry("href"));
            entries.Add(new CompletionListEntry("media"));
            entries.Add(new CompletionListEntry("ping"));

            // Microdata
            entries.Add(new CompletionListEntry("itemscope"));
            entries.Add(new CompletionListEntry("itemprop"));
            entries.Add(new CompletionListEntry("itemtype"));
            entries.Add(new CompletionListEntry("itemref"));
            entries.Add(new CompletionListEntry("itemid"));

            // Form elements
            entries.Add(new CompletionListEntry("action"));
            entries.Add(new CompletionListEntry("autocomplete"));
            entries.Add(new CompletionListEntry("autofocus"));
            entries.Add(new CompletionListEntry("checked"));
            entries.Add(new CompletionListEntry("disabled"));
            entries.Add(new CompletionListEntry("enctype"));
            entries.Add(new CompletionListEntry("for"));
            entries.Add(new CompletionListEntry("high"));
            entries.Add(new CompletionListEntry("list"));
            entries.Add(new CompletionListEntry("low"));
            entries.Add(new CompletionListEntry("max"));
            entries.Add(new CompletionListEntry("maxlength"));
            entries.Add(new CompletionListEntry("method"));
            entries.Add(new CompletionListEntry("min"));
            entries.Add(new CompletionListEntry("multiple"));
            entries.Add(new CompletionListEntry("name"));
            entries.Add(new CompletionListEntry("novalidate"));
            entries.Add(new CompletionListEntry("optimum"));
            entries.Add(new CompletionListEntry("placeholder"));
            entries.Add(new CompletionListEntry("pattern"));
            entries.Add(new CompletionListEntry("readonly"));
            entries.Add(new CompletionListEntry("required"));
            entries.Add(new CompletionListEntry("size"));
            entries.Add(new CompletionListEntry("type"));
            entries.Add(new CompletionListEntry("value"));

            // RDFa
            entries.Add(new CompletionListEntry("prefix"));
            entries.Add(new CompletionListEntry("property"));
            entries.Add(new CompletionListEntry("resource"));
            entries.Add(new CompletionListEntry("typeof"));
            entries.Add(new CompletionListEntry("vocab"));

            // Video/audio
            entries.Add(new CompletionListEntry("autoplay"));
            entries.Add(new CompletionListEntry("controls"));
            entries.Add(new CompletionListEntry("crossorigin"));
            entries.Add(new CompletionListEntry("loop"));
            entries.Add(new CompletionListEntry("mediagroup"));
            entries.Add(new CompletionListEntry("muted"));
            entries.Add(new CompletionListEntry("poster"));
            entries.Add(new CompletionListEntry("preload"));

            // Blockquote
            entries.Add(new CompletionListEntry("cite"));

            return entries;
        }
    }
}
