using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionContextProvider))]
    [Name("AttributeNameContextProvider")]
    internal class AttributeNameContextProvider : ICssCompletionContextProvider
    {

        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new Type[] { typeof(AttributeSelector), };
            }
        }

        public CssCompletionContext GetCompletionContext(ParseItem item, int position)
        {
            var attr = (AttributeSelector)item;

            int start = attr.OpenBracket.AfterEnd;
            int length = 0;

            if (attr.AttributeName != null)
            {
                start = attr.AttributeName.Start;
                length = attr.AttributeName.Length;
            }

            return new CssCompletionContext((CssCompletionContextType)605, start, length, null);
        }
    }
}
