using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    internal sealed class ColorTagger : ITagger<ColorTag>
    {
        private ITextBuffer _buffer;
        private CssTree _tree;

        internal ColorTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.ChangedLowPriority += BufferChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<ColorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || spans[0].Length == 0 || spans[0].Length >= _buffer.CurrentSnapshot.Length || !EnsureInitialized())
                yield break;

            IEnumerable<ParseItem> items = GetColors(spans[0]);

            foreach (var item in items.Where(i => (i.Start + i.Length) <= _buffer.CurrentSnapshot.Length))
            {
                SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, item.Start, item.Length);
                ColorModel colorModel = ColorParser.TryParseColor(item, ColorParser.Options.AllowAlpha | ColorParser.Options.AllowNames);
                if (colorModel != null)
                {
                    yield return new TagSpan<ColorTag>(span, new ColorTag(colorModel.Color));
                }
            }
        }

        private IEnumerable<ParseItem> GetColors(SnapshotSpan span)
        {
            List<ParseItem> items = new List<ParseItem>();
            
            // TODO: Refine this so it goes directly to the individual HexColorValue, FunctionColor and TokenItem
            ParseItem complexItem = _tree.StyleSheet.ItemFromRange(span.Start, span.Length);
            if (complexItem == null || (!(complexItem is AtDirective) && !(complexItem is RuleBlock) && !(complexItem is LessVariableDeclaration)))
                return items;

            IEnumerable<ParseItem> declarations = new ParseItem[0];

            var lessVar = complexItem as LessVariableDeclaration;

            if (lessVar != null)
            {
                declarations = new List<ParseItem>() { lessVar.Value };

            }
            else
            {
                var visitorRules = new CssItemCollector<Declaration>();
                complexItem.Accept(visitorRules);

                declarations = from d in visitorRules.Items
                               where d.Values.TextAfterEnd > span.Start && d.Values.TextStart < span.End && d.Length > 2
                               select d;
            }

            foreach (var declaration in declarations.Where(d => d != null))
            {
                var visitorHex = new CssItemCollector<HexColorValue>();
                declaration.Accept(visitorHex);
                items.AddRange(visitorHex.Items);

                var visitorFunc = new CssItemCollector<FunctionColor>();
                declaration.Accept(visitorFunc);
                items.AddRange(visitorFunc.Items);

                var visitorName = new CssItemCollector<TokenItem>();
                declaration.Accept(visitorName);
                items.AddRange(visitorName.Items.Where(i => (i.PreviousSibling == null || (i.PreviousSibling.Text != "@" && i.PreviousSibling.Text != "$")) && i.TokenType == CssTokenType.Identifier && Color.FromName(i.Text).IsNamedColor));
            }

            return items;
        }

        public bool EnsureInitialized()
        {
            if (_tree == null && WebEditor.Host != null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
                    _tree = document.Tree;
                }
                catch (ArgumentNullException)
                {
                }
            }

            return _tree != null;
        }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.Changes.Count == 0)
                return;

            var temp = TagsChanged;
            if (temp == null)
                return;

            // Combine all changes into a single span so that
            // the ITagger<>.TagsChanged event can be raised just once for a compound edit
            // with many parts.

            ITextSnapshot snapshot = e.After;

            int start = e.Changes[0].NewPosition;
            int end = e.Changes[e.Changes.Count - 1].NewEnd;

            SnapshotSpan totalAffectedSpan = new SnapshotSpan(
                snapshot.GetLineFromPosition(start).Start,
                snapshot.GetLineFromPosition(end).End);

            //temp(this, new SnapshotSpanEventArgs(totalAffectedSpan));

            Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => temp(this, new SnapshotSpanEventArgs(totalAffectedSpan))), DispatcherPriority.ApplicationIdle);
        }
    }
}