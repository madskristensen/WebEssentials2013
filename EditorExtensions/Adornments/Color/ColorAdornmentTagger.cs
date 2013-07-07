//***************************************************************************
//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//    This code is licensed under the Visual Studio SDK license terms.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//***************************************************************************

// This controls whether the adornments are positioned next to the hex values or instead of them.
#define HIDING_TEXT

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Provides color swatch adornments in place of color constants.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a sample usage of the <see cref="IntraTextAdornmentTagTransformer"/> utility class.
    /// </para>
    /// </remarks>
    internal sealed class ColorAdornmentTagger : IntraTextAdornmentTagger<ColorTag, ColorAdornment>
    {
        internal static ITagger<IntraTextAdornmentTag> GetTagger(IWpfTextView view, Lazy<ITagAggregator<ColorTag>> colorTagger)
        {
            return view.Properties.GetOrCreateSingletonProperty<ColorAdornmentTagger>(
                () => new ColorAdornmentTagger(view, colorTagger.Value));
        }

        private ITagAggregator<ColorTag> colorTagger;

        private ColorAdornmentTagger(IWpfTextView view, ITagAggregator<ColorTag> colorTagger)
            : base(view)
        {
            this.colorTagger = colorTagger;
        }

        public void Dispose()
        {
            this.colorTagger.Dispose();

            base.view.Properties.RemoveProperty(typeof(ColorAdornmentTagger));
        }

        // To produce adornments that don't obscure the text, the adornment tags
        // should have zero length spans. Overriding this method allows control
        // over the tag spans.
        protected override IEnumerable<Tuple<SnapshotSpan, PositionAffinity?, ColorTag>> GetAdornmentData(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            var colorTags = this.colorTagger.GetTags(spans);

            foreach (IMappingTagSpan<ColorTag> dataTagSpan in colorTags)
            {
                NormalizedSnapshotSpanCollection colorTagSpans = dataTagSpan.Span.GetSpans(snapshot);

                // Ignore data tags that are split by projection.
                // This is theoretically possible but unlikely in current scenarios.
                if (colorTagSpans.Count != 1)
                    continue;

                SnapshotSpan adornmentSpan = new SnapshotSpan(colorTagSpans[0].Start, 0);

                yield return Tuple.Create(adornmentSpan, (PositionAffinity?)PositionAffinity.Successor, dataTagSpan.Tag);
            }
        }

        protected override ColorAdornment CreateAdornment(ColorTag dataTag, SnapshotSpan span)
        {
            return new ColorAdornment(dataTag, view);
        }

        protected override bool UpdateAdornment(ColorAdornment adornment, ColorTag dataTag)
        {
            if (adornment != null)
            {
                adornment.Update(dataTag);
            }

            return true;
        }
    }
}
