using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

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
        }
    }

    internal sealed class Base64Tagger : ITagger<IOutliningRegionTag>
    {
        private ITextBuffer buffer;
        private CssTree _tree;

        public Base64Tagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
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
                    var items = new List<object>();
                    ImageQuickInfo.AddImageContent(items, url.UrlString.Text.Trim('"', '\''));

                    // Replace any TextBuffers into strings for the tooltip to display.
                    // This works because base64 images are loaded synchronously, so we
                    // can compute the size before returning.  If they do change, we'll
                    // need to replace them with TextBlocks & handle the Changed event.
                    for (int i = 0; i < items.Count; i++)
                    {
                        var tipBuffer = items[i] as ITextBuffer;
                        if (tipBuffer == null)
                            continue;
                        items[i] = tipBuffer.CurrentSnapshot.GetText();
                    }
                    var content = new ItemsControl { ItemsSource = items };

                    var span = new SnapshotSpan(new SnapshotPoint(buffer.CurrentSnapshot, url.UrlString.Start), url.UrlString.Length);
                    var tag = new OutliningRegionTag(true, true, url.UrlString.Length + " characters", content);
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

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.After != buffer.CurrentSnapshot)
                return;
        }
    }
}
