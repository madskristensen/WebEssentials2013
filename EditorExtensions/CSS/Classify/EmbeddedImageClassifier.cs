using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Css
{
    public static class EmbeddedImageClassificationTypes
    {
        public const string Declaration = "image.declaration";
        public const string Value = "image.value";

        [Export, Name(EmbeddedImageClassificationTypes.Declaration)]
        public static ClassificationTypeDefinition EmbeddedImageDeclarationClassificationType { get; set; }

        [Export, Name(EmbeddedImageClassificationTypes.Value)]
        public static ClassificationTypeDefinition EmbeddedImageValueClassificationType { get; set; }
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("css")]
    public sealed class EmbeddedImageClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<EmbeddedImageClassifier>(() => { return new EmbeddedImageClassifier(Registry, textBuffer); });
        }
    }

    internal sealed class EmbeddedImageClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _registry;
        private readonly ITextBuffer _buffer;
        private readonly CssTreeWatcher _tree;
        internal readonly SortedRangeList<Declaration> Cache = new SortedRangeList<Declaration>();
        private readonly IClassificationType _decClassification;
        private readonly IClassificationType _valClassification;

        internal EmbeddedImageClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _decClassification = _registry.GetClassificationType(EmbeddedImageClassificationTypes.Declaration);
            _valClassification = _registry.GetClassificationType(EmbeddedImageClassificationTypes.Value);

            _tree = CssTreeWatcher.ForBuffer(_buffer);
            _tree.TreeUpdated += TreeUpdated;
            _tree.ItemsChanged += TreeItemsChanged;
            UpdateDeclarationCache(_tree.StyleSheet);

        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> spans = new List<ClassificationSpan>();

            if (!WESettings.Instance.Css.SyncBase64ImageValues)
                return spans;

            foreach (Declaration dec in Cache.Where(d => d.PropertyName.Text.EndsWith("background-image", StringComparison.OrdinalIgnoreCase) && span.Start <= d.Start && span.End >= d.AfterEnd))
            {
                if (dec.PropertyName.Text.StartsWith("*background", StringComparison.OrdinalIgnoreCase))
                {
                    var ss = new SnapshotSpan(span.Snapshot, dec.Start, dec.Length);
                    var s = new ClassificationSpan(ss, _decClassification);
                    spans.Add(s);
                }

                if (dec.Semicolon == null)
                    continue;

                int start = dec.Colon.AfterEnd;
                int length = dec.AfterEnd - start;
                if (span.Snapshot.Length > start + length)
                {
                    var ss2 = new SnapshotSpan(span.Snapshot, start, length);
                    var s2 = new ClassificationSpan(ss2, _valClassification);
                    spans.Add(s2);
                }
            }

            return spans;
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

                var images = rule.Declarations.Where(d => d.PropertyName != null && d.PropertyName.Text.Contains("background"));

                foreach (Declaration image in images)
                {
                    if (!Cache.Contains(image))
                        Cache.Add(image);
                }

                rules.Add(rule);
            }
        }

        private void TreeUpdated(object sender, CssTreeUpdateEventArgs e)
        {
            Cache.Clear();
            UpdateDeclarationCache(e.Tree.StyleSheet);
        }

        private async void TreeItemsChanged(object sender, CssItemsChangedEventArgs e)
        {
            foreach (ParseItem item in e.DeletedItems)
            {
                if (Cache.Contains(item))
                    Cache.Remove((Declaration)item);
            }

            foreach (ParseItem item in e.InsertedItems)
            {
                UpdateDeclarationCache(item);
                await UpdateEmbeddedImageValues(item);
            }
        }

        private async Task UpdateEmbeddedImageValues(ParseItem item)
        {
            if (!WESettings.Instance.Css.SyncBase64ImageValues)
                return;

            Declaration dec = item.FindType<Declaration>();

            if (dec == null || !Cache.Contains(dec))
                return;

            var url = dec.Values.FirstOrDefault() as UrlItem;

            if (url == null || !url.IsValid || url.UrlString == null || url.UrlString.Text.Contains(";base64,"))
                return;

            var matches = Cache.Where(d => d.IsValid && d != dec && d.Parent == dec.Parent && d.Values.Any() &&
                                     (d.Values[0].NextSibling as CComment) != null);

            // Undo sometimes messes with the positions, so we have to make this check before proceeding.
            if (!matches.Any() || dec.Text.Length < dec.Colon.AfterEnd - dec.Start || dec.Colon.AfterEnd < dec.Start)
                return;

            string urlText = url.UrlString.Text.Trim('\'', '"');
            string filePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_buffer.GetFileName()), urlText));
            string b64UrlText = await FileHelpers.ConvertToBase64(filePath);
            string b64Url = url.Text.Replace(urlText, b64UrlText);
            IEnumerable<Tuple<SnapshotSpan, string>> changes = matches.Reverse().SelectMany(match =>
            {
                ParseItem value = match.Values[0];
                CComment comment = value.NextSibling as CComment;

                SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, comment.CommentText.Start, comment.CommentText.Length);

                url = value as UrlItem;

                if (url == null)
                    return null;

                SnapshotSpan b64Span = new SnapshotSpan(_buffer.CurrentSnapshot, url.Start, url.Length);

                return new[] { new Tuple<SnapshotSpan, string>(span, urlText), new Tuple<SnapshotSpan, string>(b64Span, b64Url) };
            });

            await Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                using (ITextEdit edit = _buffer.CreateEdit())
                {
                    foreach (Tuple<SnapshotSpan, string> change in changes)
                    {
                        SnapshotSpan currentSpan = change.Item1.TranslateTo(_buffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);
                        edit.Replace(currentSpan, change.Item2);
                    }

                    edit.Apply();
                }
            });
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
    [ClassificationType(ClassificationTypeNames = EmbeddedImageClassificationTypes.Declaration)]
    [Name(EmbeddedImageClassificationTypes.Declaration)]
    [Order(After = Priority.Default)]
    internal sealed class EmbeddedImageDeclarationFormatDefinition : ClassificationFormatDefinition
    {
        public EmbeddedImageDeclarationFormatDefinition()
        {
            DisplayName = "CSS Embedded Image Property";
        }
    }
}