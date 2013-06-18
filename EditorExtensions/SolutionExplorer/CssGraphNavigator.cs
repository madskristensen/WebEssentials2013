using Microsoft.CSS.Core;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace MadsKristensen.EditorExtensions
{
    class GraphNodeNavigator : IGraphNavigateToItem
    {
        public void NavigateTo(GraphObject graphObject)
        {
            GraphNode node = (GraphNode)graphObject;
            Uri file = node.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File);
            ParseItem item = node.Id.GetNestedValueByName<ParseItem>(CssGraphSchema.CssParseItem);

            if (file != null && item != null)
            {
                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(file.LocalPath);
                IWpfTextView view = ProjectHelpers.GetCurentTextView();
                SnapshotSpan span = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, item.Start, 0);
                view.Caret.MoveTo(span.Start);
                view.ViewScroller.EnsureSpanVisible(span);
            }
        }

        public int GetRank(GraphObject graphObject)
        {
            return 0;
        }
    }
}
