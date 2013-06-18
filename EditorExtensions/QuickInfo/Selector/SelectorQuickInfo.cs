using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class SelectorQuickInfo : IQuickInfoSource
    {
        private SelectorQuickInfoSourceProvider _provider;
        private ITextBuffer _buffer;
        private CssTree _tree;

        public SelectorQuickInfo(SelectorQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            _provider = provider;
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (!EnsureTreeInitialized() || session == null || qiContent == null)
                return;

            // Map the trigger point down to our buffer.
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            ParseItem item = _tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            Selector sel = item.FindType<Selector>();
            if (sel == null)
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

        /// <summary>
        /// This must be delayed so that the TextViewConnectionListener
        /// has a chance to initialize the WebEditor host.
        /// </summary>
        public bool EnsureTreeInitialized()
        {
            if (_tree == null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
                    _tree = document.Tree;
                }
                catch (Exception)
                {
                }
            }

            return _tree != null;
        }

        private bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }
}
