using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public static class ModernizrClassificationType
    {
        public const string Name = "modernizr";

        [Export, Name(ModernizrClassificationType.Name)]
        public static ClassificationTypeDefinition Definition { get; set; }
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("css")]
    public sealed class ModernizrClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new ModernizrClassifier(Registry, textBuffer));
        }
    }

    internal sealed class ModernizrClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _registry;
        private readonly ITextBuffer _buffer;
        private readonly CssTreeWatcher _tree;
        private readonly SortedRangeList<SimpleSelector> _cache = new SortedRangeList<SimpleSelector>();
        private readonly IClassificationType _modernizrClassification;

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }

        internal ModernizrClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _modernizrClassification = _registry.GetClassificationType(ModernizrClassificationType.Name);

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
    }

    [Export(typeof(EditorFormatDefinition))]
    [UserVisible(true)]
    [ClassificationType(ClassificationTypeNames = ModernizrClassificationType.Name)]
    [Name(ModernizrClassificationType.Name)]
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