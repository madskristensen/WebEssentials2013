using MadsKristensen.EditorExtensions.Completion.CompletionProviders;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.Completion.ContextProviders
{
    [Export(typeof(ICssCompletionContextProvider))]
    [Name("InputTypeCompletionContextProvider")]
    internal class InputTypeContextCompletionProvider : ICssCompletionContextProvider
    {
        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new[] { 
                    typeof(AttributeSelector), 
                };
            }
        }

        public CssCompletionContext GetCompletionContext(Microsoft.CSS.Core.ParseItem item, int position)
        {
            int equalsPosition = item.Text.IndexOf('=');
            if (equalsPosition >= 0)
            {
                string attributeName = item.Text.Substring(1, equalsPosition - 1);
                if (attributeName == "type")
                {
                    return new CssCompletionContext(InputTypeCompletionProvider.ContextTypeValue, position, item.Length, null);
                }
            }
            return null;
        }
    }
}
