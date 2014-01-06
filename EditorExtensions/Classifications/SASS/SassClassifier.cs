using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace MadsKristensen.EditorExtensions
{
    // TODO: Remove this when the SASS editor is included in VS.
    public class SassClassifier : IClassifier
    {
        private IClassificationType _variable;
        private static Regex _regex = new Regex(@"\$([a-z0-9-_]+)", RegexOptions.Compiled);

        public SassClassifier(IClassificationTypeRegistryService registry)
        {
            _variable = registry.GetClassificationType(SassClassificationTypes.Variable);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IList<ClassificationSpan> list = new List<ClassificationSpan>();

            string text = span.GetText();

            foreach (Match match in _regex.Matches(text))
            {
                SnapshotSpan s = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                list.Add(new ClassificationSpan(s, _variable));
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