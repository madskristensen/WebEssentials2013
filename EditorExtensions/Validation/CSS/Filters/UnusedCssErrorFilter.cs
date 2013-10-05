using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssErrorFilter))]
    [Name("Unused CSS Errors")]
    [Order(After = "Default")]
    internal class UnusedCssErrorFilter : ICssErrorFilter
    {
        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
        {
            for (int i = errors.Count - 1; i > -1; i--)
            {
                ICssError error = errors[i];
                if (error.Item.IsValid)
                {
                    if (error.Item.Text.StartsWith("No usages of the CSS rule \"", StringComparison.Ordinal) && error.Item.Text.EndsWith("\" have been found.", StringComparison.Ordinal))
                    {
                        errors.RemoveAt(i);
                    }
                }
            }
        }
    }
}
