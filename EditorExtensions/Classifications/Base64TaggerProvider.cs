using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Text.RegularExpressions;
using MadsKristensen.EditorExtensions.QuickInfo;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("CSS")]
    internal sealed class Base64TaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new Base64Tagger(buffer)) as ITagger<T>;
            //return new Base64Tagger(buffer) as ITagger<T>;
        }
    }

    internal sealed class Base64Tagger : ITagger<IOutliningRegionTag>
    {
        private ITextBuffer buffer;
        private ITextSnapshot snapshot;
        private string text;
        private CssTree _tree;

        public Base64Tagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            this.text = snapshot.GetText();
            this.buffer.Changed += BufferChanged;
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || !EnsureInitialized())
                yield break;

            var visitor = new CssItemCollector<UrlItem>();
            _tree.StyleSheet.Accept(visitor);

            foreach (UrlItem url in visitor.Items.Where(u => u.UrlString != null && u.Start >= spans[0].Start))
            {
                if (url.UrlString.Text.IndexOf("base64,") > -1 && buffer.CurrentSnapshot.Length >= url.UrlString.AfterEnd)
                {
                    var image = ImageQuickInfo.CreateImage(url.UrlString.Text.Trim('"', '\''));
                    var span = new SnapshotSpan(new SnapshotPoint(buffer.CurrentSnapshot, url.UrlString.Start), url.UrlString.Length);
                    var tag = new OutliningRegionTag(true, true, url.UrlString.Length + " characters", image);
                    yield return new TagSpan<IOutliningRegionTag>(span, tag);
                }
            }
        }

        public bool EnsureInitialized()
        {
            if (_tree == null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(buffer);
                    _tree = document.Tree;
                }
                catch (Exception)
                {
                }
            }

            return _tree != null;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
            if (e.After != buffer.CurrentSnapshot)
                return;
            //this.ReParse();
        }
    }
}
