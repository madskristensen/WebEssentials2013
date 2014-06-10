using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.JSON.Editor.Completion.Def;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.JSON
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name("RefValueCompletionProvider")]
    internal class RefValueCompletionProvider : IJSONCompletionListProvider
    {
        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem as JSONMember;

            if (member == null || member.Name == null || member.Name.Text != "\"$ref\"" || member.JSONDocument.Children.Count == 0)
                yield break;

            var visitor = new JSONItemCollector<JSONMember>(false);
            member.JSONDocument.Children[0].Accept(visitor);

            var definition = visitor.Items.FirstOrDefault(prop => prop.Name != null && prop.Name.Text == "\"definitions\"");

            if (definition == null || definition.Children.Count < 3)
                yield break;

            var block = definition.Children[2] as JSONBlockItem;

            var visitor2 = new JSONItemCollector<JSONMember>(false);
            block.Accept(visitor2);

            foreach (var prop in visitor2.Items)
            {
                string text = "#/definitions/" + prop.Name.Text.Trim('"');

                yield return new SimpleCompletionEntry(text, StandardGlyphGroup.GlyphReference, context.Session);
            }
        }
    }
}