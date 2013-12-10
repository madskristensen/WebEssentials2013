using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionContextProvider))]
    [Name("ClassCompletionContextProvider")]
    internal class ClassCompletionContextProvider : ICssCompletionContextProvider
    {
        public ClassCompletionContextProvider()
        {
        }

        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new Type[] { typeof(ClassSelector), };
            }
        }

        public CssCompletionContext GetCompletionContext(ParseItem item, int position)
        {
            return new CssCompletionContext((CssCompletionContextType)602, item.Start, item.Length, null);
        }
    }
}
