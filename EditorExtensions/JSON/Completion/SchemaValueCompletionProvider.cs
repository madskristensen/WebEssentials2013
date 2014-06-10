using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.JSON.Editor.Completion.Def;
using Microsoft.VisualStudio.JSON.Package.Schema;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

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

            foreach (var schema in VsJSONSchemaStore.SchemaStore.SchemaCache.Entries)
            {
                yield return new SimpleCompletionEntry(schema.OriginalPath, StandardGlyphGroup.GlyphReference, context.Session);
            }
        }
    }
}