using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionContextProvider))]
    [Name("AttributeValueCompletionContextProvider")]
    internal class AttributeValueCompletionContextProvider : ICssCompletionContextProvider
    {
        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(AttributeSelector) }; }
        }

        public CssCompletionContext GetCompletionContext(Microsoft.CSS.Core.ParseItem item, int position)
        {
            var attr = (AttributeSelector)item;

            if (attr.AttributeName == null || attr.Operation == null)
                return null;

            int start = attr.Operation.AfterEnd;
            int length = 0;

            if (attr.AttributeValue != null)
            {
                start = attr.AttributeValue.Start;
                length = attr.AttributeValue.Length;
            }

            return new CssCompletionContext(AttributeValueCompletionProvider.AttributeValue, start, length, null);
        }
    }
}
