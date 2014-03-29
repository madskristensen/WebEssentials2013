using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.JSON.Editor.Completion.Def;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.JSON
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name("SchemaValueCompletionProvider")]
    internal class SchemaValueCompletionProvider : IJSONCompletionListProvider
    {
        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem as JSONMember;

            if (member == null || member.Name == null || member.Name.Text != "\"$schema\"")
                yield break;

            string value = "http://json-schema.org/draft-04/schema#";

            yield return new JSONCompletionEntry(value, "\"" + value + "\"", "JSON hyper-schema draft v4",
            GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic),
            "iconAutomationText", true, context.Session as ICompletionSession);
        }
    }
}
