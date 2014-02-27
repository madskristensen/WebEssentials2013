using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("TagCompletionProvider")]
    internal class TagCompletionProvider : ICssCompletionListProvider
    {
        private static IEnumerable<ICssCompletionListEntry> _entryCache = GetListEntriesCache(includeUnstyled: false);
        private static HashSet<string> _basicIgnore = new HashSet<string>() { "noscript", "font", "___all___", "FlowContentElement" };
        private static HashSet<string> _cssIgnore = new HashSet<string>(_basicIgnore) { "script", "meta", "link", "style", "head", };

        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)601; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            return _entryCache;
        }

        public static IEnumerable<ICssCompletionListEntry> GetListEntriesCache(bool includeUnstyled)
        {
            var ignoreList = includeUnstyled ? _cssIgnore : _basicIgnore;
            var schemas = AttributeNameCompletionProvider.GetSchemas();

            foreach (var schema in schemas)
                foreach (var element in schema.GetTopLevelElements())
                {
                    if (!ignoreList.Contains(element.Name))
                        yield return new CompletionListEntry(element.Name) { Description = element.Description.Description };

                    foreach (var child in element.GetChildren())
                    {
                        if (!ignoreList.Contains(child.Name))
                            yield return new CompletionListEntry(child.Name) { Description = child.Description.Description };
                    }
                }
        }
    }
}
