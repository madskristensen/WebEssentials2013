using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace MadsKristensen.EditorExtensions.AppCache
{
    public class AppCacheClassifier : IClassifier
    {
        private IClassificationType _keyword, _comment;
        private static List<string> _idents = new List<string>() { "CACHE MANIFEST", "CACHE:", "NETWORK:", "FALLBACK:" };

        public AppCacheClassifier(IClassificationTypeRegistryService registry)
        {
            _keyword = registry.GetClassificationType(AppCacheClassificationTypes.Keywords);
            _comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IList<ClassificationSpan> list = new List<ClassificationSpan>();

            string text = span.GetText();

            // Comment
            int index = text.IndexOf('#');

            if (index > -1)
            {
                SnapshotSpan comment = new SnapshotSpan(span.Snapshot, span.Start + index, span.Length - index);
                list.Add(new ClassificationSpan(comment, _comment));
            }

            // Keyword
            if (_idents.Contains(text.Trim()))
            {
                list.Add(new ClassificationSpan(span, _keyword));
            }

            return list;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }
    }
}