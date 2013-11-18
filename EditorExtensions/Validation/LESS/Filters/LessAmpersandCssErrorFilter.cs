using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssErrorFilter))]
    [Name("LessAmpersandCssErrorFilter")]
    [Order(After = "Default")]
    internal class LessAmpersandCssErrorFilter : ICssErrorFilter
    {
        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
        {
            for (int i = errors.Count - 1; i > -1; i--)
            {
                ICssError error = errors[i];
                LessDeclaration dec = error.Item as LessDeclaration;

                if (dec != null && dec.Text.Contains("&"))
                {
                    errors.RemoveAt(i);
                }
            }
        }
    }
}