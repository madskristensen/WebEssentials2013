using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.JSON.Editor.Completion.Def;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.JSONLD
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name("TypeCompletionProvider")]
    internal class TypeCompletionProvider : IJSONCompletionListProvider
    {
        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem as JSONMember;

            if (member == null || member.Name == null || member.Name.Text != "\"@type\"")
                yield break;


            foreach (IVocabulary vocabulary in VocabularyFactory.GetVocabularies(member))
            {
                foreach (string key in vocabulary.Cache.Keys)
                {
                    yield return new JSONCompletionEntry(key, "\"" + key + "\"", null,
                        GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic),
                        "iconAutomationText", true, context.Session as ICompletionSession);
                }
            }
        }
    }
}
