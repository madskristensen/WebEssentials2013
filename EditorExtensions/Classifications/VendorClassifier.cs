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
    internal static class ClassificationTypes
    {
        internal const string _declaration = "vendor.declaration";
        internal const string _value = "vendor.value";

        [Export, Name(ClassificationTypes._declaration), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition VendorDeclarationClassificationType = null;

        [Export, Name(ClassificationTypes._value), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition VendorValueClassificationType = null;
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("css")]
    internal sealed class VendorClassifierProvider : IClassifierProvider
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty<VendorClassifier>(() => { return new VendorClassifier(Registry, buffer); });
        }
    }

    internal sealed class VendorClassifier : IClassifier
    {
        private IClassificationTypeRegistryService _registry;
        private ITextBuffer _buffer;
        private CssTree _tree;
        internal SortedRangeList<Declaration> Cache = new SortedRangeList<Declaration>();
        private IClassificationType _decClassification;
        private IClassificationType _valClassification;

        internal VendorClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _decClassification = _registry.GetClassificationType(ClassificationTypes._declaration);
            _valClassification = _registry.GetClassificationType(ClassificationTypes._value);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> spans = new List<ClassificationSpan>();
            if (!WESettings.GetBoolean(WESettings.Keys.SyncVendorValues) || !EnsureInitialized())
                return spans;

            var declarations = Cache.Where(d => span.End <= d.AfterEnd && d.Start >= span.Start);
            foreach (Declaration dec in Cache.Where(d => d.PropertyName.Text.Length > 0 && span.Snapshot.Length >= d.AfterEnd))
            {
                if (dec.IsVendorSpecific())
                {
                    var ss = new SnapshotSpan(span.Snapshot, dec.Start, dec.Length);
                    var s = new ClassificationSpan(ss, _decClassification);
                    spans.Add(s);
                }

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

        public string GetStandardName(Declaration dec)
        {
            string name = dec.PropertyName.Text;
            if (name.Length > 0 && name[0] == '-')
            {
                int index = name.IndexOf('-', 1) + 1;
                name = index > -1 ? name.Substring(index) : name;
            }

            return name;
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
            if (!WESettings.GetBoolean(WESettings.Keys.SyncVendorValues))
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
    [ClassificationType(ClassificationTypeNames = ClassificationTypes._declaration)]
    [Name(ClassificationTypes._declaration)]
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