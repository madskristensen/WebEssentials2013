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
        private static HashSet<string> _ignore = new HashSet<string>() { "script", "noscript", "meta", "link", "style", "head", "base", "br", "font", "___all___", "FlowContentElement" };

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
            var schemas = AttributeNameCompletionProvider.GetSchemas();

            foreach (var schema in schemas)
                foreach (var element in schema.GetTopLevelElements())
                {
                    if (!_ignore.Contains(element.Name))
                        yield return new CompletionListEntry(element.Name);
                }
        }
    }
}
