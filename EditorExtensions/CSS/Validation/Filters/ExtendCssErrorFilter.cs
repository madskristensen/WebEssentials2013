using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssErrorFilter))]
    [Name("Extend Errors")]
    [Order(After = "Default")]
    internal class ExtendCssErrorFilter : ICssErrorFilter
    {
        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
        {
            for (int i = errors.Count - 1; i > -1; i--)
            {
                ICssError error = errors[i];

                if (!(error.Item.StyleSheet is LessStyleSheet))
                    continue;

                // Remove errors from using the :extend pseudo class
                if (error.Item.Text.Contains(":extend("))
                    errors.RemoveAt(i);

                // Remove errors from using partial selectors
                else if (error.Item.PreviousSibling != null && error.Item.PreviousSibling.Text == "&")
                    errors.RemoveAt(i);
            }
        }
    }
}