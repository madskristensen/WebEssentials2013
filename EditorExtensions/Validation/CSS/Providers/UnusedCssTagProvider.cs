using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class UnusedCssTag : IErrorTag, IRange, ITagSpan<IErrorTag>
    {
        public int AfterEnd { get { return Span.Span.Start + Span.Span.Length; } }

        public string ErrorType { get; private set; }

        public int Length { get { return Span.Span.Length; } }

        public SnapshotSpan Span { get; private set; }

        public int Start { get { return Span.Span.Start; } }

        public IErrorTag Tag { get { return this; } }

        public object ToolTipContent { get; private set; }

        public static SnapshotSpan SnapshotSpanFromRule(ITextBuffer buffer, IStylingRule rule)
        {
            var snapshot = buffer.CurrentSnapshot;
            var span = new Span(rule.Offset, rule.SelectorLength);
            return new SnapshotSpan(snapshot, span);
        }

        public static UnusedCssTag FromRuleSet(ITextBuffer buffer, IStylingRule rule)
        {
            var ss = SnapshotSpanFromRule(buffer, rule);

            return new UnusedCssTag
            {
                ToolTipContent = string.Format(CultureInfo.CurrentCulture, "No usages of the CSS selector '{0}' have been found.", rule.DisplaySelectorName),
                ErrorType = "compiler warning",
                Span = ss,
            };
        }
    }

    internal class UnusedCssTagger : ITagger<IErrorTag>
    {
        private readonly ITextBuffer _buffer;

        private UnusedCssTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.PostChanged += BufferOnPostChanged;
            UsageRegistry.UsageDataUpdated += UsageRegistryOnUsageDataUpdated;
        }

        private void BufferOnPostChanged(object sender, EventArgs eventArgs)
        {
            var fileName = _buffer.GetFileName();

            if (fileName == null)
            {
                return;
            }

            var doc = DocumentFactory.GetDocument(fileName);

            if (doc == null)
            {
                return;
            }

            doc.Reparse(_buffer.CurrentSnapshot.GetText());
            UsageRegistry.Resynchronize();
        }

        private void OnTagsChanged()
        {
            if (TagsChanged != null)
            {
                var fileName = _buffer.GetFileName();

                if (fileName == null)
                {
                    return;
                }

                var s = new Span(0, _buffer.CurrentSnapshot.Length);
                var ss = new SnapshotSpan(_buffer.CurrentSnapshot, s);
                TagsChanged(this, new SnapshotSpanEventArgs(ss));
            }
        }

        private void UsageRegistryOnUsageDataUpdated(object sender, EventArgs eventArgs)
        {
            OnTagsChanged();
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public static ITagger<T> Attach<T>(ITextBuffer buffer)
            where T : ITag
        {
            return new UnusedCssTagger(buffer) as ITagger<T>;
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var fileName = _buffer.GetFileName();

            if (fileName == null)
            {
                return new ITagSpan<UnusedCssTag>[0];
            }

            var doc = DocumentFactory.GetDocument(fileName);

            if (doc == null)
            {
                return new ITagSpan<UnusedCssTag>[0];
            }

            var result = new List<ITagSpan<IErrorTag>>();

            using (AmbientRuleContext.GetOrCreate())
            {
                var applicableRules = UsageRegistry.GetAllUnusedRules(new HashSet<IStylingRule>(doc.Rules));

                foreach (var rule in applicableRules)
                {
                    try
                    {
                        result.Add(UnusedCssTag.FromRuleSet(_buffer, rule));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        //Sometimes while the document is being modified and retagging is happening, a rule's boundary will momentarily be out of sync with the document and possibly past the end
                        //  this is a condition that may be safely ignored
                    }
                }
            }

            return result;
        }
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("css")]
    [TagType(typeof(ErrorTag))]
    [Order(After = "Default Declaration")]
    internal class UnusedCssTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return UnusedCssTagger.Attach<T>(buffer);
        }
    }
}
