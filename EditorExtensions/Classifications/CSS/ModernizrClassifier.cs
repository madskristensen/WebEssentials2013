using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    internal static class ModernizrClassificationTypes
    {
        internal const string _modernizr = "modernizr";

        [Export, Name(ModernizrClassificationTypes._modernizr), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition ModernizrClassificationType = null;
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("css")]
    internal sealed class ModernizrClassifierProvider : IClassifierProvider
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty<ModernizrClassifier>(() => { return new ModernizrClassifier(Registry, buffer); });
        }
    }

    internal sealed class ModernizrClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _registry;
        private readonly ITextBuffer _buffer;
        private readonly CssTreeWatcher _tree;
        private readonly SortedRangeList<SimpleSelector> _cache = new SortedRangeList<SimpleSelector>();
        private readonly IClassificationType _modernizrClassification;

        internal ModernizrClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _modernizrClassification = _registry.GetClassificationType(ModernizrClassificationTypes._modernizr);

            _tree = CssTreeWatcher.ForBuffer(_buffer);
            _tree.TreeUpdated += TreeUpdated;
            _tree.ItemsChanged += TreeItemsChanged;
            UpdateCache(_tree.StyleSheet);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> spans = new List<ClassificationSpan>();

            foreach (SimpleSelector selector in _cache)
            {
                int start = span.Start.Position;
                int end = span.End.Position;

                if (selector.Start >= start && selector.AfterEnd <= end)
                {
                   var snapShotSpan = new SnapshotSpan(span.Snapshot, selector.Start, selector.Length);
                   var classSpan = new ClassificationSpan(snapShotSpan, _modernizrClassification);
                   spans.Add(classSpan); 
                }                 
            }

            return spans;
        }

        private void UpdateCache(ParseItem item)
        {
            var visitor = new CssItemCollector<SimpleSelector>(true);
            item.Accept(visitor);

            foreach (SimpleSelector ss in visitor.Items)
            {
                string text = ss.Text;

                if (ModernizrProvider.IsModernizr(text))
                {
                    if (!_cache.Contains(ss))
                        _cache.Add(ss);
                }
            }
        }

        private void TreeUpdated(object sender, CssTreeUpdateEventArgs e)
        {
            _cache.Clear();
            UpdateCache(e.Tree.StyleSheet);
        }

        private void TreeItemsChanged(object sender, CssItemsChangedEventArgs e)
        {
            foreach (ParseItem item in e.DeletedItems)
            {
                var matches = _cache.Where(s => s.Start >= item.Start && s.AfterEnd <= item.AfterEnd);
                foreach (var match in matches.Reverse())
                {
                    _cache.Remove(match);
                }
            }

            foreach (ParseItem item in e.InsertedItems)
            {
                UpdateCache(item);
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
    [ClassificationType(ClassificationTypeNames = ModernizrClassificationTypes._modernizr)]
    [Name(ModernizrClassificationTypes._modernizr)]
    [Order(After = Priority.Default)]
    internal sealed class ModernizrFormatDefinition : ClassificationFormatDefinition
    {
        public ModernizrFormatDefinition()
        {
            IsBold = true;
            DisplayName = "CSS Modernizr selector";
        }
    }
}