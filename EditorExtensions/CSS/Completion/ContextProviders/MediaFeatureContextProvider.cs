using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionContextProvider))]
    [Name("MediaFeatureCompletionContextProvider")]
    internal class MediaFeatureCompletionContextProvider : ICssCompletionContextProvider
    {
        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new Type[] { typeof(MediaExpression), };
            }
        }

        public CssCompletionContext GetCompletionContext(ParseItem item, int position)
        {
            MediaExpression expr = (MediaExpression)item;

            if (expr.MediaFeature != null &&
                (position >= expr.Values.TextStart && position <= expr.Values.TextAfterEnd) ||
                (expr.Colon != null && position >= expr.Colon.AfterEnd))
            {
                return new CssCompletionContext((CssCompletionContextType)611, expr.Values.TextStart, expr.Values.TextLength, null);
            }

            if (expr.OpenFunctionBrace == null || position < expr.OpenFunctionBrace.Start)
                return null;

            int length = expr.MediaFeature != null ? expr.MediaFeature.Length : 1;

            return new CssCompletionContext((CssCompletionContextType)610, expr.OpenFunctionBrace.AfterEnd, length, null);
        }
    }
}
