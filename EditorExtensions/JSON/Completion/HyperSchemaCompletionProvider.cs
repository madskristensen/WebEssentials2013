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
    [Name("HyperSchemaCompletionProvider")]
    internal class HyperSchemaCompletionProvider : IJSONCompletionListProvider
    {
        private static Dictionary<string, string> _props = new Dictionary<string, string>
        { 
            { "$schema", "Provide a URL to a JSON schema"},
            { "$ref", "Reference a predefined property"},
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
                yield return new JSONCompletionEntry(prop, "\"" + prop + "\"", _props[prop],
                GlyphService.GetGlyph(StandardGlyphGroup.GlyphExtensionMethod, StandardGlyphItem.GlyphItemPublic),
                "iconAutomationText", true, context.Session as ICompletionSession);
            }
        }
    }
}