using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("FontFamilyCompletionProvider")]
    internal class FontFamilyCompletionProvider : ICssCompletionListProvider
    {
        private static List<string> _entryCache = new List<string>()
        {
            "Arial, 'DejaVu Sans', 'Liberation Sans', Freesans, sans-serif", 
            "'Arial Narrow', 'Nimbus Sans L', sans-serif", 
            "'Arial Black', Gadget, sans-serif", 
            "'Bookman Old Style', Bookman, 'URW Bookman L', 'Palatino Linotype', serif", 
            "'Century Gothic', futura, 'URW Gothic L', Verdana, sans-serif", 
            "'Comic Sans MS', cursive", 
            "Consolas, 'Lucida Console', 'DejaVu Sans Mono', monospace", 
            "'Courier New', Courier, 'Nimbus Mono L', monospace", 
            "Constantina, Georgia, 'Nimbus Roman No9 L', serif", 
            "Helvetica, Arial, 'DejaVu Sans', 'Liberation Sans', Freesans, sans-serif", 
            "Impact,  Haettenschweiler,  'Arial Narrow Bold',  sans-serif", 
            "'Lucida Sans Unicode', 'Lucida Grande', 'Lucida Sans', 'DejaVu Sans Condensed', sans-serif", 
            "Cambria, 'Palatino Linotype', 'Book Antiqua', 'URW Palladio L', serif", 
            "symbol, 'Standard Symbols L'", 
            "Cambria, 'Times New Roman', 'Nimbus Roman No9 L', 'Freeserif', Times, serif", 
            "Verdana, Geneva, 'DejaVu Sans', sans-serif", 
            "'Monotype Corsiva', 'Apple Chancery', 'ITC Zapf Chancery', 'URW Chancery L', cursive", 
            "'Monotype Sorts', dingbats, 'ITC Zapf Dingbats', fantasy"
        };

        public CssCompletionContextType ContextType
        {
            get { return CssCompletionContextType.PropertyValue; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            Declaration dec = context.ContextItem.FindType<Declaration>();

            if (dec == null || dec.PropertyName == null || dec.PropertyName.Text != "font-family")
                yield break;

            foreach (string item in _entryCache)
            {
                ICssCompletionListEntry entry = new CompletionListEntry(item, 1);
                yield return entry;
            }
        }
    }
}
