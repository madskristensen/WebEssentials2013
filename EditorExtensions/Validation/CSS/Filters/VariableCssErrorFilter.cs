using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssErrorFilter))]
    [Name("VarCssErrorFilter")]
    [Order(After = "Default")]
    internal class VarCssErrorFilter : ICssErrorFilter
    {
        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
        {
            for (int i = errors.Count - 1; i > -1; i--)
            {
                ICssError error = errors[i];
                Declaration dec = error.Item.FindType<Declaration>();
                if (dec != null && (dec.Text.Contains("var-") || dec.Text.Contains("var(")))
                {
                    errors.RemoveAt(i);
                }
            }
        }
    }
}