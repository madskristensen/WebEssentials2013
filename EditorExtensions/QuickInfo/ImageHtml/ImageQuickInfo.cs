using System;
using System.Collections.Generic;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class ImageHtmlQuickInfo : IQuickInfoSource
    {
        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            SnapshotPoint? point = session.GetTriggerPoint(session.TextView.TextBuffer.CurrentSnapshot);

            if (!point.HasValue)
                return;

            HtmlEditorTree tree = HtmlEditorDocument.FromTextView(session.TextView).HtmlEditorTree;

            ElementNode node = null;
            AttributeNode attr = null;

            tree.GetPositionElement(point.Value.Position, out node, out attr);

            if (node == null || !node.Name.Equals("img", StringComparison.OrdinalIgnoreCase))
                return;
            if (attr == null || !attr.Name.Equals("src", StringComparison.OrdinalIgnoreCase))
                return;

            string url = ImageQuickInfo.GetFullUrl(attr.Value, session.TextView.TextBuffer);
            if (string.IsNullOrEmpty(url))
                return;

            applicableToSpan = session.TextView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(point.Value.Position, 1, SpanTrackingMode.EdgeNegative);

            ImageQuickInfo.AddImageContent(qiContent, url);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
