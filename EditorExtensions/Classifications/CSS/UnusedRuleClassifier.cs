using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions.Classifications
{
    internal static class UnusedCssClassificationTypes
    {
        internal const string _declaration = "unusedcss.rule";
        
        [Export, Name(UnusedCssClassificationTypes._declaration), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition UnusedCssClassificationType = null;
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("css")]
    internal sealed class UnusedCssClassifierProvider : IClassifierProvider
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty<UnusedCssClassifier>(() => { return new UnusedCssClassifier(Registry, buffer); });
        }
    }

    internal sealed class UnusedCssClassifier : IClassifier
    {
        private IClassificationTypeRegistryService _registry;
        private ITextBuffer _buffer;
        private CssTree _tree;
        internal SortedRangeList<Declaration> Cache = new SortedRangeList<Declaration>();
        private IClassificationType _decClassification;
        private IClassificationType _valClassification;

        internal UnusedCssClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _decClassification = _registry.GetClassificationType(UnusedCssClassificationTypes._declaration);
            UsageRegistry.UsageDataUpdated += UsageRegistry_UsageDataUpdated;
        }

        void UsageRegistry_UsageDataUpdated(object sender, EventArgs e)
        {
            _tree = null;
            var snapshotSpan = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
            RaiseClassificationChanged(snapshotSpan);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            if (!EnsureInitialized())
            {
                return new ClassificationSpan[0];
            }

            List<ClassificationSpan> spans = new List<ClassificationSpan>();
            var fileName = _buffer.GetFileName().ToLowerInvariant();

            foreach(var unusedRule in UsageRegistry.GetAllUnusedRules())
            {
                if (unusedRule.Offset + unusedRule.Length > span.Snapshot.Length || fileName != unusedRule.File.ToLowerInvariant())
                {
                    continue;
                }

                var ss = new SnapshotSpan(span.Snapshot, unusedRule.Offset, unusedRule.Length);
                var s = new ClassificationSpan(ss, _decClassification);
                spans.Add(s);
            }

            return spans;
        }

        public bool EnsureInitialized()
        {
            if (_tree == null && WebEditor.Host != null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
                    _tree = document.Tree;
                    _tree.TreeUpdated += TreeUpdated;
                    _tree.ItemsChanged += TreeItemsChanged;
                    UpdateDeclarationCache(_tree.StyleSheet);
                }
                catch (ArgumentNullException)
                {
                }
            }

            return _tree != null;
        }

        private void UpdateDeclarationCache(ParseItem item)
        {
            var visitor = new CssItemCollector<Declaration>(true);
            item.Accept(visitor);

            HashSet<RuleBlock> rules = new HashSet<RuleBlock>();

            foreach (Declaration dec in visitor.Items)
            {
                RuleBlock rule = dec.Parent as RuleBlock;
                if (rule == null || rules.Contains(rule))
                    continue;

                rules.Add(rule);
            }
        }

        private void TreeUpdated(object sender, CssTreeUpdateEventArgs e)
        {
            Cache.Clear();
            UpdateDeclarationCache(e.Tree.StyleSheet);
        }

        private void TreeItemsChanged(object sender, CssItemsChangedEventArgs e)
        {
            foreach (ParseItem item in e.DeletedItems)
            {
                if (Cache.Contains(item))
                    Cache.Remove((Declaration)item);
            }

            foreach (ParseItem item in e.InsertedItems)
            {
                UpdateDeclarationCache(item);
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public void RaiseClassificationChanged(SnapshotSpan span)
        {
            var handler = this.ClassificationChanged;
            if (handler != null)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => handler(this, new ClassificationChangedEventArgs(span))), DispatcherPriority.ApplicationIdle);
            }
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [UserVisible(true)]
    [ClassificationType(ClassificationTypeNames = UnusedCssClassificationTypes._declaration)]
    [Name(UnusedCssClassificationTypes._declaration)]
    [Order(After = Priority.Default)]
    internal sealed class UnusedCssFormatDefinition : ClassificationFormatDefinition
    {
        public UnusedCssFormatDefinition()
        {
            ForegroundOpacity = 0.5;
            DisplayName = "Unused CSS";
        }
    }
}
