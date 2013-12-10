using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssErrorFilter))]
    [Name("MsFilterCssErrorFilter")]
    [Order(After = "Default")]
    internal class MsFilterCssErrorFilter : ICssErrorFilter
    {
        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
        {
            for (int i = errors.Count - 1; i > -1; i--)
            {
                ICssError error = errors[i];
                Declaration dec = error.Item.FindType<Declaration>();
                if (dec != null && dec.IsValid && dec.PropertyName.Text == "-ms-filter")
                {
                    errors.RemoveAt(i);
                    errors.Insert(i, CreateNewError(error));
                }
            }
        }

        private static SimpleErrorTag CreateNewError(ICssError error)
        {
            string message = error.Text + " " + " The value must be wrapped in single or double quotation marks.";
            return new SimpleErrorTag(error.Item, message, CssErrorFlags.TaskListError | CssErrorFlags.UnderlineRed);
        }
    }
}