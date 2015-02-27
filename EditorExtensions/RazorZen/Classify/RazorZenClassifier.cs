using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions.RazorZen
{
    public class RazorZenClassifier : IClassifier
    {
        private static List<char> zenAttributes = new List<char>() { '#', '.' };
        private static List<string> commentStarts = new List<string>() { "!-", "@*" };
        private static List<string> commentEnds = new List<string>() { "-!", "*@" };
        private IClassificationType _razorStart;
        private IClassificationType _comment;
        private IClassificationType _zenTag;
        private IClassificationType _zenAttributeName;
        private IClassificationType _zenAttributeValue;

        public RazorZenClassifier(IClassificationTypeRegistryService registry)
        {
            _razorStart = registry.GetClassificationType(RazorZenClassificationTypes.RazorStart);
            _comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _zenTag = registry.GetClassificationType(RazorZenClassificationTypes.ZenTag);
            _zenAttributeName = registry.GetClassificationType(RazorZenClassificationTypes.ZenAttributName);
            _zenAttributeValue = registry.GetClassificationType(RazorZenClassificationTypes.ZenAttributValue);
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();

            var text = span.GetText();

            {  // Razor Starts
                var i = 0;
                while (i < text.Length)
                {
                    i = text.IndexOf("@", i);

                    if (i == -1) break;

                    var razorStart = new SnapshotSpan(span.Snapshot, span.Start + i, 1);
                    list.Add(new ClassificationSpan(razorStart, _razorStart));

                    i++;
                }

                i = 0;
                while (i < text.Length)
                {
                    i = text.IndexOf("@*", i);

                    if (i == -1) break;

                    var razorStart = new SnapshotSpan(span.Snapshot, span.Start + i, 2);
                    list.Add(new ClassificationSpan(razorStart, _razorStart));

                    i += 2;
                }

                i = 0;
                while (i < text.Length)
                {
                    i = text.IndexOf("*@", i);

                    if (i == -1) break;

                    var razorStart = new SnapshotSpan(span.Snapshot, span.Start + i, 2);
                    list.Add(new ClassificationSpan(razorStart, _razorStart));

                    i += 2;
                }
            }

            {  // Razor Comments
                var start = text.IndexOf("@*");
                var end = text.LastIndexOf("*@");

                if (start > -1 && end > -1 && start < end)
                {
                    var comment = new SnapshotSpan(span.Snapshot, span.Start + start + 2, (end - start) - 2);
                    list.Add(new ClassificationSpan(comment, _comment));
                }
            }

            {  // Zen Comments
                var start = text.IndexOf("!-");
                var end = text.LastIndexOf("-!");

                if (start > -1 && end > -1 && start < end)
                {
                    var comment = new SnapshotSpan(span.Snapshot, span.Start + start, span.Length - (start - end));
                    list.Add(new ClassificationSpan(comment, _comment));
                }
            }

            { //Tag Names
                var tagsRegex = new Regex(" ([\\w|-|_]+)[#|\\.|\\[|{| |\r|\n]",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                // Match the regular expression pattern against a text string.
                var m = tagsRegex.Match(text);

                while (m.Success)
                {
                    var g = m.Groups[1];

                    var numberOfOpens = text.Substring(0, g.Index).Split(new[] { "[", "{", "(", "!-", "@*" }, StringSplitOptions.None).Length - 1;
                    var numberOfCloses = text.Substring(0, g.Index).Split(new[] { "]", "}", ")", "-!", "*@" }, StringSplitOptions.None).Length - 1;

                    if (numberOfOpens == numberOfCloses)
                    {
                        var zenTagName = new SnapshotSpan(span.Snapshot, span.Start + g.Index, g.Length);
                        list.Add(new ClassificationSpan(zenTagName, _zenTag));
                    }

                    m = m.NextMatch();
                }
            }

            { // Attribut Values from id and classes
                var tagsRegex = new Regex("[#|\\.]([\\w|\\-|_]+[^#\\.\\[{ ])",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                // Match the regular expression pattern against a text string.
                var m = tagsRegex.Match(text);

                while (m.Success)
                {
                    var g = m.Groups[1];

                    var numberOfOpens = text.Substring(0, g.Index).Split(new[] { "[", "{", "(", "!-", "@*" }, StringSplitOptions.None).Length - 1;
                    var numberOfCloses = text.Substring(0, g.Index).Split(new[] { "]", "}", ")", "-!", "*@" }, StringSplitOptions.None).Length - 1;

                    if (numberOfOpens == numberOfCloses)
                    {
                        var zenTAttributValue = new SnapshotSpan(span.Snapshot, span.Start + g.Index, g.Length);
                        list.Add(new ClassificationSpan(zenTAttributValue, _zenAttributeValue));
                    }
                    m = m.NextMatch();
                }
            }

            { // Attribute Names
                for (int i = 0; i < text.Length; i++)
                {
                    var c = text[i];

                    if (zenAttributes.Contains(c))
                    {
                        var numberOfOpens = text.Substring(0, i).Split(new[] { "[", "{", "(", "!-", "@*" }, StringSplitOptions.None).Length - 1;
                        var numberOfCloses = text.Substring(0, i).Split(new[] { "]", "}", ")", "-!", "*@" }, StringSplitOptions.None).Length - 1;

                        if (numberOfOpens == numberOfCloses)
                        {
                            var zenAttributeName = new SnapshotSpan(span.Snapshot, span.Start + i, 1);
                            list.Add(new ClassificationSpan(zenAttributeName, _zenAttributeName));
                        }
                    }
                }
            }

            {  // Attribut Names & Values
                var i = 0;
                while (i < text.Length)
                {
                    var attributesStart = text.IndexOf("[", i);
                    if (attributesStart == -1) break;

                    var attributesEnd = text.IndexOf("]", attributesStart + 1);
                    if (attributesEnd == -1) break;

                    var ii = attributesStart + 1;
                    while (ii < attributesEnd)
                    {
                        var startValue = text.IndexOf("=\"", ii);
                        if (startValue == -1) break;

                        var endValue = text.IndexOf(" ", startValue + 1);
                        if (endValue == -1)
                        {
                            endValue = attributesEnd - 1;
                        }

                        var zenAttributeValueStart = new SnapshotSpan(span.Snapshot, span.Start + ii, startValue - ii);
                        list.Add(new ClassificationSpan(zenAttributeValueStart, _zenAttributeName));

                        var zenAttributeValueEnd = new SnapshotSpan(span.Snapshot, span.Start + startValue, endValue - startValue);
                        list.Add(new ClassificationSpan(zenAttributeValueEnd, _zenAttributeValue));

                        ii = endValue + 1;
                    }

                    var zenAttributesStart = new SnapshotSpan(span.Snapshot, span.Start + attributesStart, 1);
                    list.Add(new ClassificationSpan(zenAttributesStart, _zenAttributeValue));

                    var zenAttributesEnd = new SnapshotSpan(span.Snapshot, span.Start + attributesEnd, 1);
                    list.Add(new ClassificationSpan(zenAttributesEnd, _zenAttributeValue));

                    i = attributesEnd + 1;
                }
            }
            return list;
        }
    }
}