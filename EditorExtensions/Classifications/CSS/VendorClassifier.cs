using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public static class VendorClassificationTypes
    {
        public const string Declaration = "vendor.declaration";
        public const string Value = "vendor.value";

        [Export, Name(VendorClassificationTypes.Declaration)]
        public static ClassificationTypeDefinition VendorDeclarationClassificationType { get; set; }

        [Export, Name(VendorClassificationTypes.Value)]
        public static ClassificationTypeDefinition VendorValueClassificationType { get; set; }
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("css")]
    public sealed class VendorClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<VendorClassifier>(() => { return new VendorClassifier(Registry, textBuffer); });
        }
    }

    internal sealed class VendorClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _registry;
        private readonly ITextBuffer _buffer;
        private readonly CssTreeWatcher _tree;
        internal readonly SortedRangeList<Declaration> Cache = new SortedRangeList<Declaration>();
        private readonly IClassificationType _decClassification;
        private readonly IClassificationType _valClassification;

        internal VendorClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _decClassification = _registry.GetClassificationType(VendorClassificationTypes.Declaration);
            _valClassification = _registry.GetClassificationType(VendorClassificationTypes.Value);

            _tree = CssTreeWatcher.ForBuffer(_buffer);
            _tree.TreeUpdated += TreeUpdated;
            _tree.ItemsChanged += TreeItemsChanged;
            UpdateDeclarationCache(_tree.StyleSheet);

        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> spans = new List<ClassificationSpan>();
            if (!WESettings.Instance.Css.SyncVendorValues)
                return spans;

            foreach (Declaration dec in Cache.Where(d => d.PropertyName.Text.Length > 0 && span.Start <= d.Start && span.End >= d.AfterEnd))
            {
                if (dec.IsVendorSpecific())
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

        public static string GetStandardName(Declaration dec)
        {
            string name = dec.PropertyName.Text;
            if (name.Length > 0 && name[0] == '-')
            {
                int index = name.IndexOf('-', 1) + 1;
                name = index > -1 ? name.Substring(index) : name;
            }

            return name;
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

                var vendors = rule.Declarations.Where(d => d.IsValid && d.IsVendorSpecific());
                foreach (Declaration vendor in vendors)
                {
                    string name = GetStandardName(vendor);
                    Declaration standard = rule.Declarations.FirstOrDefault(d => d.IsValid && d.PropertyName.Text == name);

                    if (standard != null)
                    {
                        if (!Cache.Contains(standard))
                            Cache.Add(standard);

                        if (GetValueText(standard) == GetValueText(vendor) && !Cache.Contains(vendor))
                            Cache.Add(vendor);
                    }
                }

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
                UpdateVendorValues(item);
            }
        }

        private void UpdateVendorValues(ParseItem item)
        {
            if (!WESettings.Instance.Css.SyncVendorValues)
                return;

            Declaration dec = item.FindType<Declaration>();
            if (dec != null && Cache.Contains(dec) && !dec.IsVendorSpecific())
            {
                // Find all vendor specifics that isn't the standard property.
                var matches = Cache.Where(d => d.IsValid && d != dec && d.Parent == dec.Parent && GetStandardName(d) == dec.PropertyName.Text && d.PropertyName.Text != dec.PropertyName.Text);

                // Undo sometimes messes with the positions, so we have to make this check before proceeding.
                if (!matches.Any() || dec.Text.Length < dec.Colon.AfterEnd - dec.Start || dec.Colon.AfterEnd < dec.Start)
                    return;

                string text = dec.Text.Substring(dec.Colon.AfterEnd - dec.Start, dec.AfterEnd - dec.Colon.AfterEnd);
                using (ITextEdit edit = _buffer.CreateEdit())
                {
                    foreach (Declaration match in matches.Reverse())
                    {
                        SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, match.Colon.AfterEnd, match.AfterEnd - match.Colon.AfterEnd);
                        if (span.GetText() != text)
                            edit.Replace(span, text);
                    }

                    edit.Apply();
                }
            }
        }

        private string GetValueText(Declaration dec)
        {
            int start = dec.Colon.AfterEnd;
            int length = dec.AfterEnd - start;
            return _buffer.CurrentSnapshot.GetText(start, length);
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
    [ClassificationType(ClassificationTypeNames = VendorClassificationTypes.Declaration)]
    [Name(VendorClassificationTypes.Declaration)]
    [Order(After = Priority.Default)]
    internal sealed class VendorDeclarationFormatDefinition : ClassificationFormatDefinition
    {
        public VendorDeclarationFormatDefinition()
        {
            ForegroundOpacity = 0.5;
            DisplayName = "CSS Vendor Property";
        }
    }
}