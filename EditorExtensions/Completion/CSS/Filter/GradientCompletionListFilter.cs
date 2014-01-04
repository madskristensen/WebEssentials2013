using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Completion
{
    [Export(typeof(ICssCompletionListFilter))]
    [Name("Gradient Filter")]
    internal class GradientCompletionListFilter : ICssCompletionListFilter
    {
        [Import]
        private IGlyphService _glyphService = null;

        public void FilterCompletionList(IList<CssCompletionEntry> completions, CssCompletionContext context)
        {
            if (context.ContextType != CssCompletionContextType.PropertyValue)
                return;

            for (int i = 0; i < completions.Count; i++)
            {
                CssSchemaCompletionEntry entry = completions[i] as CssSchemaCompletionEntry;

                if (entry != null && entry.DisplayText.Contains("gradient("))
                {
                    var cce = CreateCompletionEntry(context, entry);
                    cce.FilterType = entry.FilterType;
                    cce.IsBuilder = entry.IsBuilder;

                    completions[i] = cce;
                }
            }
        }

        private CssSchemaCompletionEntry CreateCompletionEntry(CssCompletionContext context, CssSchemaCompletionEntry entry)
        {
            CustomCompletionListEntry interim = new CustomCompletionListEntry(entry.DisplayText, GetArguments(entry.DisplayText));
            interim.Description = entry.Description;

            object[] parameters = new object[]
            {
                interim,
                entry.CompletionProvider,
                CssTextSource.Document,
                context.Snapshot.CreateTrackingSpan(context.SpanStart, context.SpanLength, SpanTrackingMode.EdgeExclusive),
                _glyphService
            };

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            return (CssSchemaCompletionEntry)Activator.CreateInstance(typeof(CssSchemaCompletionEntry), flags, null, parameters, null);
        }

        private static string GetArguments(string functionName)
        {
            switch (functionName)
            {
                case "linear-gradient()":
                case "-webkit-linear-gradient()":
                case "-ms-linear-gradient()":
                case "-moz-linear-gradient()":
                case "-o-linear-gradient()":
                    return functionName.Replace("()", "(top,  #1e5799 0%, #7db9e8 100%)");

                case "-webkit-gradient()":
                    return functionName.Replace("()", "(linear, left top, left bottom, color-stop(0%,#1e5799), color-stop(100%,#7db9e8))");

                case "radial-gradient()":
                case "-webkit-radial-gradient()":
                case "-ms-radial-gradient()":
                case "-moz-radial-gradient()":
                case "-o-radial-gradient()":
                    return functionName.Replace("()", "(50px 50px, circle closest-side, black, white)");

                case "repeating-linear-gradient()":
                case "-webkit-repeating-linear-gradient()":
                case "-ms-repeating-linear-gradient()":
                case "-moz-repeating-linear-gradient()":
                case "-o-repeating-linear-gradient()":
                    return functionName.Replace("()", "(red, blue 20px, red 40px)");

                case "repeating-radial-gradient()":
                case "-webkit-repeating-radial-gradient()":
                case "-ms-repeating-radial-gradient()":
                case "-moz-repeating-radial-gradient()":
                case "-o-repeating-radial-gradient()":
                    return functionName.Replace("()", "(red, blue 20px, red 40px)");
            }

            return functionName;
        }
    }
}