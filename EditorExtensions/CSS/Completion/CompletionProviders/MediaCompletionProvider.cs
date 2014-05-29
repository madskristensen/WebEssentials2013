using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("MediaCompletionProvider")]
    internal class MediaCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)610; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            MediaExpression expression = (MediaExpression)context.ContextItem;

            yield return new CompletionListEntry("device-width");
            yield return new CompletionListEntry("max-device-width");
            yield return new CompletionListEntry("min-device-width");

            yield return new CompletionListEntry("device-height");
            yield return new CompletionListEntry("max-device-height");
            yield return new CompletionListEntry("min-device-height");

            yield return new CompletionListEntry("height");
            yield return new CompletionListEntry("max-height");
            yield return new CompletionListEntry("min-height");

            yield return new CompletionListEntry("width");
            yield return new CompletionListEntry("max-width");
            yield return new CompletionListEntry("min-width");

            yield return new CompletionListEntry("orientation");
            yield return new CompletionListEntry("scan");
            yield return new CompletionListEntry("grid");

            yield return new CompletionListEntry("resolution");
            yield return new CompletionListEntry("max-resolution");
            yield return new CompletionListEntry("min-resolution");

            yield return new CompletionListEntry("aspect-ratio");
            yield return new CompletionListEntry("max-aspect-ratio");
            yield return new CompletionListEntry("min-aspect-ratio");
            yield return new CompletionListEntry("device-aspect-ratio");
            yield return new CompletionListEntry("max-device-aspect-ratio");
            yield return new CompletionListEntry("min-device-aspect-ratio");

            yield return new CompletionListEntry("color");
            yield return new CompletionListEntry("color-index");
            yield return new CompletionListEntry("min-color");
            yield return new CompletionListEntry("max-color");

            yield return new CompletionListEntry("color-index");
            yield return new CompletionListEntry("max-color-index");
            yield return new CompletionListEntry("min-color-index");

            yield return new CompletionListEntry("update-frequency");
            yield return new CompletionListEntry("overflow-block");
            yield return new CompletionListEntry("overflow-inline");

            yield return new CompletionListEntry("monochrome");
            yield return new CompletionListEntry("max-monochrome");
            yield return new CompletionListEntry("min-monochrome");

            yield return new CompletionListEntry("pointer");
            yield return new CompletionListEntry("hover");
            yield return new CompletionListEntry("light-level");
            yield return new CompletionListEntry("scripting");

            // Internet Explorer
            yield return new CompletionListEntry("-ms-high-contrast");

            // Webkit
            yield return new CompletionListEntry("-webkit-device-pixel-ratio");
            yield return new CompletionListEntry("-webkit-max-device-pixel-ratio");
            yield return new CompletionListEntry("-webkit-min-device-pixel-ratio");
        }
    }
}
