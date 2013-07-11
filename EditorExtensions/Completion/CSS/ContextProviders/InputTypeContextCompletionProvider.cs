using MadsKristensen.EditorExtensions.Completion.CompletionProviders;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

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
            // Test cases
            //
            // The following should be ignored
            //   input[wibble=""] {}    // ignore: not "type" attribute
            //   a[type=""] {}          // ignore: not "input" element. 
            //                          // This case isn't handled correctly as it is an edge case and hugely complicates the handling of LESS scenarios such as 
            //                          // input { &.myclass {  &[type=""] { } } }
            //
            // The following should be handled
            //   input[type=""] {}
            //   input[type=] {}                // note the lack of quotes - attributeSelector.AttributeValue is null
            //   input[type|=""] {}
            //   foo input[type=""] {}
            //   input[type=""] foo {}
            //   input[foo="bar"][type=""] {}
            //   input.myclass[type=""] {}

            var attr = (AttributeSelector)item;

            if (attr.AttributeName.Text != "type" || attr.Operation == null)
                return null;

            int start = attr.Operation.AfterEnd;
            int length = 0;

            if (attr.AttributeValue != null)
            {
                start = attr.AttributeValue.Start;
                length = attr.AttributeValue.Length;
            }
            
            return new CssCompletionContext(InputTypeCompletionProvider.InputTypeValue, start, length, null);
        }
    }
}
