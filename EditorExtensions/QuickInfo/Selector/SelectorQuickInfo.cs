using System.Collections.Generic;
using System.Text;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class SelectorQuickInfo : IQuickInfoSource
    {
        private ITextBuffer _buffer;

        public SelectorQuickInfo(ITextBuffer subjectBuffer)
        {
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (session == null || qiContent == null)
                return;

            // Map the trigger point down to our buffer.
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            var tree = CssEditorDocument.FromTextBuffer(_buffer);
            ParseItem item = tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            Selector sel = item.FindType<Selector>();
            if (sel == null)
                return;
            // Mixins don't have specificity
            if (sel.SimpleSelectors.Count == 1 && sel.SimpleSelectors[0].SubSelectors.Count == 1 && sel.SimpleSelectors[0].SubSelectors[0] is LessMixinDeclaration)
                return;

            applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(item.Start, item.Length, SpanTrackingMode.EdgeNegative);

            string content = GenerateContent(sel);
            qiContent.Add(content);
        }

        private static string GenerateContent(Selector sel)
        {
            SelectorSpecificity specificity = new SelectorSpecificity(sel);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Selector specificity:\t\t" + specificity.ToString());
            //sb.AppendLine(" - IDs:\t\t\t\t" + specificity.IDs);
            //sb.AppendLine(" - Classes:\t\t\t" + (specificity.Classes + specificity.PseudoClasses));
            //sb.AppendLine(" - Attributes:\t\t" + specificity.Attributes);
            //sb.AppendLine(" - Elements:\t\t" + (specificity.Elements + specificity.PseudoElements));

            return sb.ToString().Trim();
        }

        public void Dispose() { }
    }
}
