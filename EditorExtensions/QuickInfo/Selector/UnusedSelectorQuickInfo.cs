using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.CSS.Core;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using Microsoft.CSS.Editor;

namespace MadsKristensen.EditorExtensions.QuickInfo.Selector
{
    internal class UnusedSelectorQuickInfo : IQuickInfoSource
    {
        private UnusedSelectorQuickInfoSourceProvider _provider;
        private ITextBuffer _buffer;
        private CssTree _tree;

        public UnusedSelectorQuickInfo(UnusedSelectorQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
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

            RuleSet rule = item.FindType<RuleSet>();
            if (rule == null)
                return;

            applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(item.Start, item.Length, SpanTrackingMode.EdgeNegative);
            var fileName = _buffer.GetFileName().ToLowerInvariant();
            var unmatchedEntry = UsageRegistry.GetAllUnusedRules().FirstOrDefault(x => x.File == fileName && x.Is(rule));

            if(unmatchedEntry == null)
            {
                return;
            }

            qiContent.Add("No usages of this rule have been found");
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
