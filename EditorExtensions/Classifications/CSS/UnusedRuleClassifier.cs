using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions.Classifications
{
    internal static class UnusedCssClassificationTypes
    {
        internal const string Declaration = "unusedcss.rule";
        
        [Export, Name(Declaration), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
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
            return buffer.Properties.GetOrCreateSingletonProperty(() => new UnusedCssClassifier(Registry, buffer));
        }
    }

    internal sealed class UnusedCssClassifier : IClassifier, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private CssTree _tree;
        internal SortedRangeList<Declaration> Cache = new SortedRangeList<Declaration>();
        private readonly IClassificationType _decClassification;

        internal UnusedCssClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            var currentFile = buffer.GetFileName().ToLowerInvariant();
            _buffer = buffer;
            _decClassification = registry.GetClassificationType(UnusedCssClassificationTypes.Declaration);

            if (!string.IsNullOrEmpty(currentFile))
            {
                UsageRegistry.UsageDataUpdated += ResyncClassificationSpans;
            }
        }

        private bool _isInManualUpdate;

        void ResyncClassificationSpans(object sender, EventArgs e)
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

            if (string.IsNullOrEmpty(currentFile))
            {
                return new ClassificationSpan[0];
            }

            var document = DocumentFactory.GetDocument(currentFile);

            if (document == null)
            {
                return new ClassificationSpan[0];
            }

            var spans = new List<ClassificationSpan>();
            var sheetRules = new HashSet<IStylingRule>(document.Rules);

            if (document.IsProcessingUnusedCssRules)
            {
                using (AmbientRuleContext.GetOrCreate())
                {
                    foreach (var unusedRule in UsageRegistry.GetAllUnusedRules(sheetRules))
                    {
                        if (unusedRule.Offset + unusedRule.Length > span.Snapshot.Length)
                        {
                            continue;
                        }

                        var ss = new SnapshotSpan(span.Snapshot, unusedRule.Offset, unusedRule.SelectorLength);
                        var s = new ClassificationSpan(ss, _decClassification);
                        spans.Add(s);
                    }
                }
            }

            return spans;
        }

        private CssEditorDocument _document;

        public bool EnsureInitialized()
        {
            if (_tree == null && WebEditor.Host != null)
            {
                try
                {
                    _document = CssEditorDocument.FromTextBuffer(_buffer);
                    _tree = _document.Tree;
                    _buffer.PostChanged += _buffer_PostChanged;
                }
                catch (ArgumentNullException)
                {
                }
            }

            return _tree != null;
        }

        private void _buffer_PostChanged(object sender, EventArgs e)
        {
            ReparseSheet();
        }

        private static readonly object Sync = new object();

        private void ReparseSheet()
        {
            if (_tree.StyleSheet.ContainsParseErrors)
            {
                return;
            }

            try
            {
                if (!EnsureInitialized())
                {
                    return;
                }

                var currentFile = _buffer.GetFileName().ToLowerInvariant();
                var document = DocumentFactory.GetDocument(currentFile);
                var documentText = _tree.TextProvider.Text;

                if (document != null && documentText != null)
                {
                    if (document.IsProcessingUnusedCssRules)
                    {
                        lock (Sync)
                        {
                            document.Reparse(documentText);
                        }

                        UsageRegistry.Resync();
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                //Swallow exceptions...
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public void RaiseClassificationChanged(SnapshotSpan span)
        {
            var handler = ClassificationChanged;
            if (handler != null)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => handler(this, new ClassificationChangedEventArgs(span))), DispatcherPriority.ApplicationIdle);
            }
        }

        public void Dispose()
        {
            UsageRegistry.UsageDataUpdated -= ResyncClassificationSpans;
        }

        ~UnusedCssClassifier()
        {
            Dispose();
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [UserVisible(true)]
    [ClassificationType(ClassificationTypeNames = UnusedCssClassificationTypes.Declaration)]
    [Name(UnusedCssClassificationTypes.Declaration)]
    [Order(After = Priority.Default)]
    internal sealed class UnusedCssFormatDefinition : ClassificationFormatDefinition
    {
        public UnusedCssFormatDefinition()
        {
            DisplayName = "CSS Unused Rules";
            TextDecorations = new TextDecorationCollection {SquigglyHelper.Squiggly(Colors.SteelBlue)};
        }
    }
}
