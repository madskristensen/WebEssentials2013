using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MadsKristensen.EditorExtensions.JSON;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.JSON.Editor.Completion.Def;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.JSONLD
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name("PropertyNameCompletionProvider")]
    internal class PropertyNameCompletionProvider : IJSONCompletionListProvider
    {
        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyName; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem as JSONMember;

            if (member == null || member.Name == null)
                yield break;

            var vocabularies = VocabularyFactory.GetVocabularies(member);

            if (!vocabularies.Any())
                yield break;

            JSONBlockItem block = member.FindType<JSONBlockItem>();

            var visitor = new JSONItemCollector<JSONMember>();
            block.Accept(visitor);

            JSONMember ldType = visitor.Items.FirstOrDefault(m => m.Name != null && m.Value != null && m.Name.Text == "\"@type\"");

            if (ldType == null)
                yield break;

            string value = ldType.Value.Text.Trim('"');

            foreach (IVocabulary vocab in vocabularies.Where(v => v.Cache.ContainsKey(value)))
                foreach (Entry entry in vocab.Cache[value])
                    yield return new JSONCompletionEntry(entry.Name, "\"" + entry.Name + "\"", null,
                        entry.Glyph, "iconAutomationText", true, context.Session as ICompletionSession);
        }
    }
}