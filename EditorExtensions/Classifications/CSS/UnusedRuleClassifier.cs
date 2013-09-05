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

        internal UnusedCssClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _registry = registry;
            _buffer = buffer;
            _decClassification = _registry.GetClassificationType(UnusedCssClassificationTypes._declaration);
            UsageRegistry.UsageDataUpdated += UsageRegistry_UsageDataUpdated;
        }

        private bool _isInManualUpdate;

        void UsageRegistry_UsageDataUpdated(object sender, EventArgs e)
        {
            if (_isInManualUpdate)
            {
                return;
            }

            _isInManualUpdate = true;
            var snapshotSpan = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
            RaiseClassificationChanged(snapshotSpan);
            _isInManualUpdate = false;
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            if (!EnsureInitialized())
            {
                return new ClassificationSpan[0];
            }


            var currentFile = _buffer.GetFileName().ToLowerInvariant();
            var document = DocumentFactory.GetDocument(currentFile);

            if (document == null)
            {
                return new ClassificationSpan[0];
            }

            List<ClassificationSpan> spans = new List<ClassificationSpan>();
            var fileName = _buffer.GetFileName().ToLowerInvariant();
            var sheetRules = new HashSet<IStylingRule>(document.Rules);

            foreach(var unusedRule in UsageRegistry.GetAllUnusedRules(sheetRules))
            {
                if (unusedRule.Offset + unusedRule.Length > span.Snapshot.Length)
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
                    _tree.TreeUpdated += _tree_TreeUpdated;
                    _tree.ItemsChanged += _tree_ItemsChanged;
                }
                catch (ArgumentNullException)
                {
                }
            }

            return _tree != null;
        }

        private static readonly object _sync = new object();

        private void ReparseSheet()
        {
            if (_tree.StyleSheet.ContainsParseErrors)
            {
                return;
            }

            try
            {
                _isInManualUpdate = true;

                if (!EnsureInitialized())
                {
                    return;
                }

                var currentFile = _buffer.GetFileName().ToLowerInvariant();
                var document = CssDocument.For(currentFile);
                var documentText = _tree.TextProvider.Text;

                if (document != null && documentText != null)
                {
                    lock (_sync)
                    {
                        document.Reparse(documentText);
                    }

                    UsageRegistry.Resync();
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                //Swallow exceptions...
            }
            finally
            {
                _isInManualUpdate = false;
            }
        }

        private void _tree_ItemsChanged(object sender, CssItemsChangedEventArgs e)
        {
            ReparseSheet();
        }

        private async void _tree_TreeUpdated(object sender, CssTreeUpdateEventArgs e)
        {
            ReparseSheet();
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
