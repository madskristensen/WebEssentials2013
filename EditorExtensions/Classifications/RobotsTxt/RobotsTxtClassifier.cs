using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Export(typeof(IClassifierProvider))]
    [ContentType(RobotsTxtContentTypeDefinition.RobotsTxtContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class RobotsTxtClassifierProvider : IClassifierProvider, IVsTextViewCreationListener
    {
        [Import]
        internal IClassificationTypeRegistryService Registry { get; set; }

        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<RobotsTxtClassifier>(() => new RobotsTxtClassifier(Registry));
        }

        public void VsTextViewCreated(Microsoft.VisualStudio.TextManager.Interop.IVsTextView textViewAdapter)
        {
            ITextDocument document;
            RobotsTxtClassifier classifier;

            var view = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            view.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document);
            
            if (document != null && document.FilePath.EndsWith("\\robots.txt"))
            {
                view.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(RobotsTxtClassifier), out classifier);

                if (classifier != null)
                {
                    ITextSnapshot snapshot = view.TextBuffer.CurrentSnapshot;
                    classifier.RaiseClassificationChanged(new SnapshotSpan(snapshot, 0, snapshot.Length));
                }
            }
        }
    }

    public class RobotsTxtClassifier : IClassifier
    {
        private IClassificationType _keyword, _comment;
        private bool _isRobotsTxt = false;
        private HashSet<string> _valid = new HashSet<string>() { "user-agent", "disallow", "sitemap", "crawl-delay", "host" };

        public RobotsTxtClassifier(IClassificationTypeRegistryService registry)
        {
            _keyword = registry.GetClassificationType(RobotsTxtClassificationTypes.RobotsTxtKeyword);
            _comment = registry.GetClassificationType(RobotsTxtClassificationTypes.RobotsTxtComment);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IList<ClassificationSpan> list = new List<ClassificationSpan>();
            if (!_isRobotsTxt)
                return list;

            string text = span.GetText();
            int index = text.IndexOf("#");

            if (index > -1)
            {
                var result = new SnapshotSpan(span.Snapshot, span.Start + index, text.Length - index);
                list.Add(new ClassificationSpan(result, _comment));
            }
            
            if (index == -1 || index > 0)
            {
                string[] args = text.Split(':');
                
                if (args.Length >= 2 && _valid.Contains(args[0].Trim().ToLowerInvariant()))
                {
                    var result = new SnapshotSpan(span.Snapshot, span.Start, args[0].Length);
                    list.Add(new ClassificationSpan(result, _keyword));
                }
            }

            return list;
        }

        public void RaiseClassificationChanged(SnapshotSpan span)
        {
            _isRobotsTxt = true;
            var handler = this.ClassificationChanged;
            
            if (handler != null)
                handler(this, new ClassificationChangedEventArgs(span));
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    }
}
