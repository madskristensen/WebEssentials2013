using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace MadsKristensen.EditorExtensions.WebVtt
{
    public class WebVttClassifier : IClassifier
    {
        private IClassificationType _markup, _name, _statement, _time, _comment;
        private static Regex _rxTime = new Regex(@"\d{2}:\d{2}:\d{2}\.\d{3}(\s)+-->(\s)+()\d{2}:\d{2}:\d{2}\.\d{3}", RegexOptions.Compiled);
        private static Regex _rxMarkup = new Regex(@"<(/|)([^>/]+)>", RegexOptions.Compiled);
        private static Regex _rxName = new Regex(@"<v ([^>]+)>", RegexOptions.Compiled);

        public WebVttClassifier(IClassificationTypeRegistryService registry)
        {
            _markup = registry.GetClassificationType(WebVttClassificationTypes.Markup);
            _name = registry.GetClassificationType(WebVttClassificationTypes.Name);
            _statement = registry.GetClassificationType(WebVttClassificationTypes.Statement);
            _time = registry.GetClassificationType(WebVttClassificationTypes.Time);
            _comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
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

            // Comment
            if (text.StartsWith("NOTE", StringComparison.Ordinal))
            {
                list.Add(new ClassificationSpan(span, _comment));
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

                // Name
                if (match.Value.StartsWith("<v ", StringComparison.Ordinal))
                {
                    Match nameMatch = _rxName.Match(match.Value);
                    if (nameMatch.Success)
                    {
                        start = span.Start.Position + match.Index + nameMatch.Groups[1].Index;
                        SnapshotSpan nameSpan = new SnapshotSpan(span.Snapshot, start, nameMatch.Groups[1].Length);
                        list.Add(new ClassificationSpan(nameSpan, _name));
                    }
                }
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