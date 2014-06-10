using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.JSON.Editor.Completion.Def;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.JSON
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name("HyperSchemaCompletionProvider")]
    internal class HyperSchemaCompletionProvider : IJSONCompletionListProvider
    {
        private static Dictionary<string, string> _props = new Dictionary<string, string>
        { 
            { "$ref", "Reference a predefined property"},
            // Add this when the issue with duplicate completion entries has been fixed.
            // The issue occurs when editing a JSON schema file, since it already has $schema in the completion list.
            //{ "$schema", "Provide a URL to a JSON schema"},
        };

        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyName; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem as JSONMember;

            if (member == null || member.Name == null || !member.Name.Text.Trim('"').StartsWith("$"))
                yield break;

            foreach (string prop in _props.Keys)
            {
                yield return new SimpleCompletionEntry(prop, _props[prop], StandardGlyphGroup.GlyphReference, context.Session);
            }
        }
    }
}