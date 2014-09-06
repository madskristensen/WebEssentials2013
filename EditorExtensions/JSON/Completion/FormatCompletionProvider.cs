using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.JSON.Editor.Completion.Def;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.JSON
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name("FormatCompletionProvider")]
    internal class FormatCompletionProvider : IJSONCompletionListProvider
    {
        private static List<string> _props = new List<string>
        {
            "date-time",
            "email",
            "hostname",
            "ipv4",
            "ipv6",
            "regex",
            "uri",
         };

        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem as JSONMember;

            if (member == null || member.Name == null || member.UnquotedNameText != "format")
                yield break;

            foreach (string prop in _props)
            {
                yield return new SimpleCompletionEntry(prop, "\"" + prop + "\"", context.Session);
            }
        }
    }
}