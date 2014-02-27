using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionContextProvider))]
    [Name("VariableContextProvider")]
    internal class VariableContextProvider : ICssCompletionContextProvider
    {
        public VariableContextProvider()
        {
        }

        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new Type[] { typeof(Function), };
            }
        }

        public CssCompletionContext GetCompletionContext(ParseItem item, int position)
        {
            Function func = (Function)item;
            if (func.FunctionName == null && func.FunctionName.Text != "var-")
                return null;

            return new CssCompletionContext((CssCompletionContextType)609, func.Arguments.TextStart, func.Arguments.TextLength, null);
        }
    }
}
