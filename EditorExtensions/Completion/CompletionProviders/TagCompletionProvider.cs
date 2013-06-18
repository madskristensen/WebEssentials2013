using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("TagCompletionProvider")]
    internal class TagCompletionProvider : ICssCompletionListProvider
    {
        private static IEnumerable<ICssCompletionListEntry> _entryCache = GetListEntriesCache();

        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)601; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            return _entryCache;
        }

        private static IEnumerable<ICssCompletionListEntry> GetListEntriesCache()
        {
            List<ICssCompletionListEntry> entries = new List<ICssCompletionListEntry>();

            entries.Add(new CompletionListEntry("a"));
            entries.Add(new CompletionListEntry("abbr"));
            entries.Add(new CompletionListEntry("acronym"));
            entries.Add(new CompletionListEntry("address"));
            //entries.Add(new CompletionListEntry("applet"));
            entries.Add(new CompletionListEntry("area"));
            entries.Add(new CompletionListEntry("article"));
            entries.Add(new CompletionListEntry("aside"));
            entries.Add(new CompletionListEntry("audio"));
            entries.Add(new CompletionListEntry("b"));
            //entries.Add(new CompletionListEntry("base"));
            //entries.Add(new CompletionListEntry("basefont"));
            entries.Add(new CompletionListEntry("bdi"));
            entries.Add(new CompletionListEntry("bdo"));
            entries.Add(new CompletionListEntry("big"));
            entries.Add(new CompletionListEntry("blockquote"));
            entries.Add(new CompletionListEntry("body"));
            //entries.Add(new CompletionListEntry("br"));
            entries.Add(new CompletionListEntry("button"));
            entries.Add(new CompletionListEntry("canvas"));
            entries.Add(new CompletionListEntry("caption"));
            entries.Add(new CompletionListEntry("center"));
            entries.Add(new CompletionListEntry("cite"));
            entries.Add(new CompletionListEntry("code"));
            entries.Add(new CompletionListEntry("col"));
            entries.Add(new CompletionListEntry("colgroup"));
            //entries.Add(new CompletionListEntry("command"));
            entries.Add(new CompletionListEntry("datalist"));
            entries.Add(new CompletionListEntry("dd"));
            entries.Add(new CompletionListEntry("del"));
            entries.Add(new CompletionListEntry("details"));
            entries.Add(new CompletionListEntry("dfn"));
            //entries.Add(new CompletionListEntry("dir"));
            entries.Add(new CompletionListEntry("div"));
            entries.Add(new CompletionListEntry("dl"));
            entries.Add(new CompletionListEntry("dt"));
            entries.Add(new CompletionListEntry("em"));
            entries.Add(new CompletionListEntry("embed"));
            entries.Add(new CompletionListEntry("fieldset"));
            entries.Add(new CompletionListEntry("figcaption"));
            entries.Add(new CompletionListEntry("figure"));
            //entries.Add(new CompletionListEntry("font"));
            entries.Add(new CompletionListEntry("footer"));
            entries.Add(new CompletionListEntry("form"));
            //entries.Add(new CompletionListEntry("frame"));
            //entries.Add(new CompletionListEntry("frameset"));
            entries.Add(new CompletionListEntry("h1"));
            entries.Add(new CompletionListEntry("h2"));
            entries.Add(new CompletionListEntry("h3"));
            entries.Add(new CompletionListEntry("h4"));
            entries.Add(new CompletionListEntry("h5"));
            entries.Add(new CompletionListEntry("h6"));
            //entries.Add(new CompletionListEntry("head"));
            entries.Add(new CompletionListEntry("header"));
            entries.Add(new CompletionListEntry("hgroup"));
            entries.Add(new CompletionListEntry("hr"));
            entries.Add(new CompletionListEntry("html"));
            entries.Add(new CompletionListEntry("i"));
            entries.Add(new CompletionListEntry("iframe"));
            entries.Add(new CompletionListEntry("img"));
            entries.Add(new CompletionListEntry("input"));
            entries.Add(new CompletionListEntry("ins"));
            //entries.Add(new CompletionListEntry("keygen"));
            entries.Add(new CompletionListEntry("kbd"));
            entries.Add(new CompletionListEntry("label"));
            entries.Add(new CompletionListEntry("legend"));
            entries.Add(new CompletionListEntry("li"));
            //entries.Add(new CompletionListEntry("link"));
            entries.Add(new CompletionListEntry("map"));
            entries.Add(new CompletionListEntry("mark"));
            entries.Add(new CompletionListEntry("menu"));
            //entries.Add(new CompletionListEntry("meta"));
            entries.Add(new CompletionListEntry("meter"));
            entries.Add(new CompletionListEntry("nav"));
            //entries.Add(new CompletionListEntry("noframes"));
            //entries.Add(new CompletionListEntry("noscript"));
            entries.Add(new CompletionListEntry("object"));
            entries.Add(new CompletionListEntry("ol"));
            entries.Add(new CompletionListEntry("optgroup"));
            entries.Add(new CompletionListEntry("option"));
            entries.Add(new CompletionListEntry("output"));
            entries.Add(new CompletionListEntry("p"));
            entries.Add(new CompletionListEntry("param"));
            entries.Add(new CompletionListEntry("pre"));
            entries.Add(new CompletionListEntry("progress"));
            entries.Add(new CompletionListEntry("q"));
            entries.Add(new CompletionListEntry("rp"));
            entries.Add(new CompletionListEntry("rt"));
            entries.Add(new CompletionListEntry("ruby"));
            entries.Add(new CompletionListEntry("s"));
            entries.Add(new CompletionListEntry("samp"));
            //entries.Add(new CompletionListEntry("script"));
            entries.Add(new CompletionListEntry("section"));
            entries.Add(new CompletionListEntry("select"));
            entries.Add(new CompletionListEntry("small"));
            //entries.Add(new CompletionListEntry("source"));
            entries.Add(new CompletionListEntry("span"));
            //entries.Add(new CompletionListEntry("strike"));
            entries.Add(new CompletionListEntry("strong"));
            entries.Add(new CompletionListEntry("style"));
            entries.Add(new CompletionListEntry("sub"));
            entries.Add(new CompletionListEntry("summary"));
            entries.Add(new CompletionListEntry("sup"));
            entries.Add(new CompletionListEntry("svg"));
            entries.Add(new CompletionListEntry("table"));
            entries.Add(new CompletionListEntry("tbody"));
            entries.Add(new CompletionListEntry("td"));
            entries.Add(new CompletionListEntry("textarea"));
            entries.Add(new CompletionListEntry("tfoot"));
            entries.Add(new CompletionListEntry("th"));
            entries.Add(new CompletionListEntry("thead"));
            entries.Add(new CompletionListEntry("time"));
            //entries.Add(new CompletionListEntry("title"));
            entries.Add(new CompletionListEntry("tr"));
            entries.Add(new CompletionListEntry("track"));
            entries.Add(new CompletionListEntry("tt"));
            entries.Add(new CompletionListEntry("u"));
            entries.Add(new CompletionListEntry("ul"));
            entries.Add(new CompletionListEntry("var"));
            entries.Add(new CompletionListEntry("video"));
            //entries.Add(new CompletionListEntry("wbr"));

            return entries;
        }
    }
}
