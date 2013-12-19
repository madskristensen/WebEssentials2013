using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace MadsKristensen.EditorExtensions
{
    public class WebVttClassifier : IClassifier
    {
        private IClassificationType _markup, _statement, _time;
        private static Regex _rxTime = new Regex(@"\d{1,}:\d{2}:\d{2}\.\d{3}", RegexOptions.Compiled);
        private static Regex _rxMarkup = new Regex(@"<([^>]+)>", RegexOptions.Compiled);

        public WebVttClassifier(IClassificationTypeRegistryService registry)
        {
            _markup = registry.GetClassificationType(WebVttClassificationTypes.Markup);
            _statement = registry.GetClassificationType(WebVttClassificationTypes.Statement);
            _time = registry.GetClassificationType(WebVttClassificationTypes.Time);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IList<ClassificationSpan> list = new List<ClassificationSpan>();

            string text = span.GetText();

            // Statement
            if (text.Trim().Equals("WEBVTT", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new ClassificationSpan(span, _statement));
                return list;
            }

            // Time
            foreach (Match match in _rxTime.Matches(text))
            {
                int start = span.Start.Position + match.Index;
                SnapshotSpan timeSpan = new SnapshotSpan(span.Snapshot, start, match.Length);
                list.Add(new ClassificationSpan(timeSpan, _time));
            }

            if (list.Count > 0)
                return list;

            // Markup
            foreach (Match match in _rxMarkup.Matches(text))
            {
                int start = span.Start.Position + match.Index;
                SnapshotSpan timeSpan = new SnapshotSpan(span.Snapshot, start, match.Length);
                list.Add(new ClassificationSpan(timeSpan, _markup));
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