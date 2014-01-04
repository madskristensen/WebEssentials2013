using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("LessExtractHexVariableSmartTagProvider")]
    internal class LessExtractHexVariableSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(HexColorValue); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            LessRuleBlock rule = item.FindType<LessRuleBlock>();

            if (!item.IsValid || rule == null)
                yield break;

            yield return new LessExtractVariableSmartTagAction(itemTrackingSpan, item);
        }
    }

    [Export(typeof(ICssSmartTagProvider))]
    [Name("LessExtractRgbVariableSmartTagProvider")]
    internal class LessExtractRgbVariableSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(FunctionColor); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            LessRuleBlock rule = item.FindType<LessRuleBlock>();

            if (!item.IsValid || rule == null)
                yield break;

            yield return new LessExtractVariableSmartTagAction(itemTrackingSpan, item);
        }
    }

    [Export(typeof(ICssSmartTagProvider))]
    [Name("LessExtractNameVariableSmartTagProvider")]
    internal class LessExtractNameVariableSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(TokenItem); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            TokenItem token = (TokenItem)item;

            if (!token.IsValid || token.TokenType != CssTokenType.Identifier || token.FindType<Declaration>() == null || !(item.StyleSheet is LessStyleSheet))
                yield break;

            var color = Color.FromName(token.Text);
            if (color.IsKnownColor)
            {
                yield return new LessExtractVariableSmartTagAction(itemTrackingSpan, item);
            }
        }
    }
}
