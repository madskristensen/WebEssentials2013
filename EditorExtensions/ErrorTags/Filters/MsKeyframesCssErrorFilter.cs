using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssErrorFilter))]
    [Name("MsKeyframesCssErrorFilter")]
    [Order(After = "Default")]
    internal class MsKeyframesCssErrorFilter : ICssErrorFilter
    {
        private static readonly string _message = " IE only supportes the standard @keyframes implementation.";

        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
        {
            for (int i = errors.Count - 1; i > -1; i--)
            {
                ICssError error = errors[i];
                if (error.Item.IsValid)
                {
                    AtDirective atDir = error.Item.FindType<AtDirective>();
                    if (atDir != null && atDir.IsValid && atDir.Keyword.Text == "-ms-keyframes")
                    {
                        errors.RemoveAt(i);
                        ICssError tag = new SimpleErrorTag(error.Item, error.Text + _message, CssErrorFlags.TaskListError | CssErrorFlags.UnderlineRed);
                        errors.Insert(i, tag);
                    }
                }
            }
        }
    }
}