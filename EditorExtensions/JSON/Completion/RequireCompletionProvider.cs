using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.JSON.Editor.Completion.Def;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.JSON
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name("RequireCompletionProvider")]
    internal class RequireCompletionProvider : IJSONCompletionListProvider
    {
        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.ArrayElement; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            JSONMember req = context.ContextItem.FindType<JSONMember>();

            if (req == null || req.Name == null || req.Name.Text != "\"required\"")
                yield break;

            var propVisitor = new JSONItemCollector<JSONMember>();
            req.Parent.Accept(propVisitor);

            var props = propVisitor.Items.FirstOrDefault(i => i.Name.Text == "\"properties\"");

            if (props == null)
                yield break;

            var child = props.Children[2] as JSONBlockItem;

            if (child == null)
                yield break;

            var visitor = new JSONItemCollector<JSONMember>();
            child.Accept(visitor);

            foreach (var item in visitor.Items)
            {
                yield return new SimpleCompletionEntry(item.Name.Text.Trim('"'), context.Session);
            }
        }
    }
}