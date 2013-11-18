using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssErrorFilter))]
    [Name("Custom Errors")]
    [Order(After = "Default")]
    internal class CustomCssErrorFilter : ICssErrorFilter
    {
        private readonly Dictionary<string, string> _messsages = new Dictionary<string, string>()
        {
            { "cursorhand", "Consider using \"pointer\" instead." },
            { "cursornormal", "Consider using \"default\" instead." }
        };

        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
        {
            for (int i = errors.Count - 1; i > -1; i--)
            {
                ICssError error = errors[i];
                if (error.Item.IsValid)
                {
                    Declaration dec = error.Item.FindType<Declaration>();
                    if (dec != null && dec.IsValid && dec.PropertyName.Text == "cursor")
                    {
                        if (error.Item.Text == "hand")
                        {
                            errors.RemoveAt(i);
                            errors.Insert(i, CreateNewError(error, "cursorhand"));
                        }
                        else if (error.Item.Text == "normal")
                        {
                            errors.RemoveAt(i);
                            errors.Insert(i, CreateNewError(error, "cursornormal"));
                        }
                    }
                }
            }
        }

        private SimpleErrorTag CreateNewError(ICssError error, string messageKey)
        {
            string message = error.Text + " " + _messsages[messageKey];
            return new SimpleErrorTag(error.Item, message, CssErrorFlags.TaskListError | CssErrorFlags.UnderlineRed);
        }
    }
}