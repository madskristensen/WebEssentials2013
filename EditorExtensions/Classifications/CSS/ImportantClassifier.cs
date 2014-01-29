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
    public static class ImportantClassificationType
    {
        public const string Name = "css-important";

        [Export, Name(ImportantClassificationType.Name)]
        public static ClassificationTypeDefinition Definition { get; set; }
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("css")]
    public sealed class ImportantClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new ImportantClassifier(Registry, textBuffer));
        }
    }

    internal sealed class ImportantClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _registry;
        private readonly ITextBuffer _buffer;
        private readonly CssTreeWatcher _tree;
        private readonly SortedRangeList<TokenItem> _cache = new SortedRangeList<TokenItem>();
        private readonly IClassificationType _importantClassification;

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }

        internal ImportantClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _importantClassification = _registry.GetClassificationType(ImportantClassificationType.Name);

            _tree = CssTreeWatcher.ForBuffer(_buffer);
            _tree.TreeUpdated += TreeUpdated;
            _tree.ItemsChanged += TreeItemsChanged;
            UpdateCache(_tree.StyleSheet);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> spans = new List<ClassificationSpan>();

            foreach (TokenItem item in _cache)
            {
                int start = span.Start.Position;
                int end = span.End.Position;

                if (item.Start >= start && item.AfterEnd <= end)
                {
                    var snapShotSpan = new SnapshotSpan(span.Snapshot, item.Start - 1, item.Length + 1);
                    var classSpan = new ClassificationSpan(snapShotSpan, _importantClassification);
                    spans.Add(classSpan);
                }
            }

            return spans;
        }

        private void UpdateCache(ParseItem item)
        {
            var visitor = new CssItemCollector<Declaration>(true);
            item.Accept(visitor);

            foreach (TokenItem token in visitor.Items.Where(d => d.Important != null).Select(d => d.Important))
            {
                if (!_cache.Contains(token))
                    _cache.Add(token);
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
    [ClassificationType(ClassificationTypeNames = ImportantClassificationType.Name)]
    [Name(ImportantClassificationType.Name)]
    [Order(After = Priority.Default)]
    internal sealed class ImportantFormatDefinition : ClassificationFormatDefinition
    {
        public ImportantFormatDefinition()
        {
            IsBold = true;
            DisplayName = "CSS !important";
        }
    }
}