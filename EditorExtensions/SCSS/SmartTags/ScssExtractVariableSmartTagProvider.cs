using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using MadsKristensen.EditorExtensions.Css;
using MadsKristensen.EditorExtensions.Less;
using Microsoft.CSS.Core;
using Microsoft.Scss.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Scss
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("ScssExtractHexVariableSmartTagProvider")]
    internal class ScssExtractHexVariableSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(HexColorValue); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            ScssRuleBlock rule = item.FindType<ScssRuleBlock>();

            if (!item.IsValid || rule == null)
                yield break;

            yield return new LessExtractVariableSmartTagAction(itemTrackingSpan, item, "$");
        }
    }

    [Export(typeof(ICssSmartTagProvider))]
    [Name("ScssExtractRgbVariableSmartTagProvider")]
    internal class ScssExtractRgbVariableSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(FunctionColor); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            ScssRuleBlock rule = item.FindType<ScssRuleBlock>();

            if (!item.IsValid || rule == null)
                yield break;

            yield return new LessExtractVariableSmartTagAction(itemTrackingSpan, item, "$");
        }
    }

    [Export(typeof(ICssSmartTagProvider))]
    [Name("ScssExtractNameVariableSmartTagProvider")]
    internal class ScssExtractNameVariableSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(TokenItem); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            TokenItem token = (TokenItem)item;

            if (!token.IsValid || token.TokenType != CssTokenType.Identifier || token.FindType<Declaration>() == null || !(item.StyleSheet is ScssStyleSheet))
                yield break;

            var color = Color.FromName(token.Text);
            if (color.IsKnownColor)
            {
                yield return new LessExtractVariableSmartTagAction(itemTrackingSpan, item, "$");
            }
        }
    }
}
