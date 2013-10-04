using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssErrorFilter))]
    [Name("LessExtendCssErrorFilter")]
    [Order(After = "Default")]
    internal class LessExtendCssErrorFilter : ICssErrorFilter
    {
        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
        {
            for (int i = errors.Count - 1; i > -1; i--)
            {
                ICssError error = errors[i];
                
                if (error.Item.StyleSheet is LessStyleSheet && error.Text.Contains(":extend("))
                {
                    errors.RemoveAt(i);                
                }
            }
        }
    }
}