using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionContextProvider))]
    [Name("UrlPickerCompletionContextProvider")]
    internal class UrlPickerCompletionContextProvider : ICssCompletionContextProvider
    {
        public UrlPickerCompletionContextProvider()
        {
        }

        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new Type[] { typeof(UrlItem), };
            }
        }

        public CssCompletionContext GetCompletionContext(ParseItem item, int position)
        {
            UrlItem urlItem = (UrlItem)item;
            int start = item.Start + 4;
            int length = 0;

            if (urlItem.UrlString != null)
            {
                start = urlItem.UrlString.Start;
                length = urlItem.UrlString.Length;

                int relative = position - start;
                int lastSlash = urlItem.UrlString.Text.LastIndexOf('/');
                if (lastSlash < relative)
                {
                    start = start + lastSlash + 1;
                    length = length - (lastSlash + 1);
                }
            }

            return new CssCompletionContext((CssCompletionContextType)604, start, length, null);
        }
    }
}
