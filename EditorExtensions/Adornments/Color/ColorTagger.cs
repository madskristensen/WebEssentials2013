﻿using MadsKristensen.EditorExtensions;
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

namespace IntraTextAdornmentSample
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
            ParseItem complexItem = _tree.StyleSheet.ItemFromRange(span.Start, span.Length);
            if (complexItem == null || (!(complexItem is AtDirective) && !(complexItem is RuleBlock) && !(complexItem is LessVariableDeclaration) && !(complexItem is FunctionArgument)))
                return Enumerable.Empty<ParseItem>();

            var colorCrawler = new CssItemAggregator<ParseItem>(filter: e => e.AfterEnd > span.Start && e.Start < span.End)
            {
                (HexColorValue h) => h,
                (FunctionColor c) => c,
                (TokenItem i) => (i.PreviousSibling == null || (i.PreviousSibling.Text != "@" && i.PreviousSibling.Text != "$"))    // Ignore variable names that happen to be colors
                               && i.TokenType == CssTokenType.Identifier
                               && (i.FindType<Declaration>() != null || i.FindType<LessExpression>() != null)                       // Ignore classnames that happen to be colors
                               && Color.FromName(i.Text).IsNamedColor 
                               ? i : null
            };

            return colorCrawler.Crawl(complexItem).Where(o => o != null);

            //IEnumerable<ParseItem> declarations;
            //var lessVar = complexItem as LessVariableDeclaration;

            //if (lessVar != null)
            //{
            //    declarations = new[] { lessVar.Value };
            //}
            //else
            //{
            //    declarations = new CssItemAggregator<ParseItem>(filter: e => e.AfterEnd > span.Start && e.Start < span.End)
            //    {
            //        (LessMixinArgument a) => a.Argument,
            //        (LessMixinDeclarationArgument a) => a.Variable.Value,
            //        (FunctionArgument a) => a.ArgumentItems
            //    }.Crawl(complexItem).Where(d => d != null);
            //}
            //TODO: This doesn't seem to work correctly, probably because I'm reusing the mutable crawler.
            //return declarations.SelectMany(colorCrawler.Crawl).Where(o => o != null);
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