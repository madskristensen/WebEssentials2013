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

            var attributeSelector = (AttributeSelector)item;
            if (attributeSelector.AttributeName.Text != "type")
            {
                return null;
            }
            var attributeValue = attributeSelector.AttributeValue;
            int start;
            int length;
            if (attributeValue == null)
            {
                start = attributeSelector.AttributeName.AfterEnd;
                length = 0;
            }
            else
            {
                start = attributeValue.Start;
                length = attributeValue.Length;

                string attributeValueText = attributeValue.Text;
                if (!string.IsNullOrEmpty(attributeValueText))
                {
                    if (attributeValue.Text.StartsWith("\""))
                    {
                        // ignore leading quote
                        start += 1;
                        length -= 1;
                    }
                    if (attributeValue.Text.EndsWith("\""))
                    {
                        // ignore trailing quote
                        length -= 1;
                    }
                }
            }
            return new CssCompletionContext(InputTypeCompletionProvider.ContextTypeValue, start, length, null);
        }
    }
}
