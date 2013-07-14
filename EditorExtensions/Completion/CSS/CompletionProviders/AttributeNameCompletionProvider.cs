using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("AttributeNameCompletionProvider")]
    internal class AttributeNameCompletionProvider : ICssCompletionListProvider
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
            // Global
            yield return new CompletionListEntry("accesskey");
            yield return new CompletionListEntry("class");
            yield return new CompletionListEntry("contenteditable");
            yield return new CompletionListEntry("contextmenu");
            yield return new CompletionListEntry("dir");
            yield return new CompletionListEntry("draggable");
            yield return new CompletionListEntry("dropzone");
            yield return new CompletionListEntry("hidden");
            yield return new CompletionListEntry("id");
            yield return new CompletionListEntry("lang");
            yield return new CompletionListEntry("spellcheck");
            yield return new CompletionListEntry("style");
            yield return new CompletionListEntry("tabindex");
            yield return new CompletionListEntry("title");
            yield return new CompletionListEntry("translate");

            // Common
            yield return new CompletionListEntry("alt");
            yield return new CompletionListEntry("height");
            yield return new CompletionListEntry("rel");
            yield return new CompletionListEntry("role");
            yield return new CompletionListEntry("width");
            yield return new CompletionListEntry("src");

            // A element
            yield return new CompletionListEntry("target");
            yield return new CompletionListEntry("href");
            yield return new CompletionListEntry("media");
            yield return new CompletionListEntry("ping");

            // Microdata
            yield return new CompletionListEntry("itemscope");
            yield return new CompletionListEntry("itemprop");
            yield return new CompletionListEntry("itemtype");
            yield return new CompletionListEntry("itemref");
            yield return new CompletionListEntry("itemid");

            // Form elements
            yield return new CompletionListEntry("action");
            yield return new CompletionListEntry("autocomplete");
            yield return new CompletionListEntry("autofocus");
            yield return new CompletionListEntry("checked");
            yield return new CompletionListEntry("disabled");
            yield return new CompletionListEntry("enctype");
            yield return new CompletionListEntry("for");
            yield return new CompletionListEntry("high");
            yield return new CompletionListEntry("list");
            yield return new CompletionListEntry("low");
            yield return new CompletionListEntry("max");
            yield return new CompletionListEntry("maxlength");
            yield return new CompletionListEntry("method");
            yield return new CompletionListEntry("min");
            yield return new CompletionListEntry("multiple");
            yield return new CompletionListEntry("name");
            yield return new CompletionListEntry("novalidate");
            yield return new CompletionListEntry("optimum");
            yield return new CompletionListEntry("placeholder");
            yield return new CompletionListEntry("pattern");
            yield return new CompletionListEntry("readonly");
            yield return new CompletionListEntry("required");
            yield return new CompletionListEntry("size");
            yield return new CompletionListEntry("type");
            yield return new CompletionListEntry("value");

            // RDFa
            yield return new CompletionListEntry("prefix");
            yield return new CompletionListEntry("property");
            yield return new CompletionListEntry("resource");
            yield return new CompletionListEntry("typeof");
            yield return new CompletionListEntry("vocab");

            // Video/audio
            yield return new CompletionListEntry("autoplay");
            yield return new CompletionListEntry("controls");
            yield return new CompletionListEntry("crossorigin");
            yield return new CompletionListEntry("loop");
            yield return new CompletionListEntry("mediagroup");
            yield return new CompletionListEntry("muted");
            yield return new CompletionListEntry("poster");
            yield return new CompletionListEntry("preload");

            // Blockquote
            yield return new CompletionListEntry("cite");
        }
    }
}
