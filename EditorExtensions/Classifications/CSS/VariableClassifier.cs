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
    public static class VariableClassificationType
    {
        public const string Name = "variable";

        [Export, Name(VariableClassificationType.Name)]
        public static ClassificationTypeDefinition Definition { get; set; }
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("css")]
    public sealed class VariableClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new VariableClassifier(Registry, textBuffer));
        }
    }

    internal sealed class VariableClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _registry;
        private readonly ITextBuffer _buffer;
        private readonly CssTreeWatcher _tree;
        private readonly SortedRangeList<Declaration> _cache = new SortedRangeList<Declaration>();
        private readonly IClassificationType _variableClassification;

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }

        internal VariableClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _variableClassification = _registry.GetClassificationType(VariableClassificationType.Name);

            _tree = CssTreeWatcher.ForBuffer(_buffer);
            _tree.TreeUpdated += TreeUpdated;
            _tree.ItemsChanged += TreeItemsChanged;
            UpdateCache(_tree.StyleSheet);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> spans = new List<ClassificationSpan>();
            int start = span.Start.Position;
            int end = span.End.Position;

            foreach (Declaration dec in _cache.Skip(_cache.FindInsertIndex(start, true)))
            {
                var snapShotSpan = new SnapshotSpan(span.Snapshot, dec.PropertyName.Start, dec.PropertyName.Length);
                var classSpan = new ClassificationSpan(snapShotSpan, _variableClassification);

                spans.Add(classSpan);

                if (dec.PropertyName.AfterEnd > end)
                    break;
            }

            return spans;
        }

        private void UpdateCache(ParseItem item)
        {
            var visitor = new CssItemCollector<Declaration>(true);
            item.Accept(visitor);

            foreach (Declaration dec in visitor.Items)
            {
                string text = dec.Text;

                if (text.StartsWith("var-", StringComparison.Ordinal))
                {
                    if (!_cache.Contains(dec))
                        _cache.Add(dec);
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
    [ClassificationType(ClassificationTypeNames = VariableClassificationType.Name)]
    [Name(VariableClassificationType.Name)]
    [Order(After = Priority.Default)]
    internal sealed class VariableFormatDefinition : ClassificationFormatDefinition
    {
        public VariableFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Purple;
            DisplayName = "CSS Variable Declaration";
        }
    }
}