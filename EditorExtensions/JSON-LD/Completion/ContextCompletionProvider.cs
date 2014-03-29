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
    [Name("ContextCompletionProvider")]
    internal class ContextCompletionProvider : IJSONCompletionListProvider
    {
        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem as JSONMember;

            if (member == null || member.Name == null || member.Name.Text != "\"@context\"")
                yield break;

            var vocabularies = VocabularyFactory.GetAllVocabularies();

            foreach (IVocabulary vocabulary in vocabularies)
            {
                yield return new JSONCompletionEntry(vocabulary.DisplayName, "\"" + vocabulary.DisplayName + "\"", null,
                    GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic),
                    "iconAutomationText", true, context.Session as ICompletionSession);
            }
        }
    }
}
