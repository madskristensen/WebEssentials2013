using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("MediaValueCompletionProvider")]
    internal class MediaValueCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)611; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            MediaExpression expression = (MediaExpression)context.ContextItem;

            switch (expression.MediaFeature.Text)
            {
                case "orientation":
                    yield return new CompletionListEntry("portrait");
                    yield return new CompletionListEntry("landscape");
                    break;

                case "scan":
                    yield return new CompletionListEntry("interlace");
                    yield return new CompletionListEntry("progressive");
                    break;

                case "-ms-high-contrast":
                    yield return new CompletionListEntry("active");
                    yield return new CompletionListEntry("black-on-white");
                    yield return new CompletionListEntry("white-on-black");
                    yield return new CompletionListEntry("none");
                    break;

                case "update-frequency":
                    yield return new CompletionListEntry("none");
                    yield return new CompletionListEntry("normal");
                    yield return new CompletionListEntry("slow");
                    break;

                case "overflow-block":
                    yield return new CompletionListEntry("none");
                    yield return new CompletionListEntry("scroll");
                    yield return new CompletionListEntry("optional-paged");
                    yield return new CompletionListEntry("paged");
                    break;

                case "overflow-inline":
                    yield return new CompletionListEntry("none");
                    yield return new CompletionListEntry("scroll");
                    break;

                case "pointer":
                    yield return new CompletionListEntry("none");
                    yield return new CompletionListEntry("coarse");
                    yield return new CompletionListEntry("fine");
                    break;

                case "hover":
                    yield return new CompletionListEntry("none");
                    yield return new CompletionListEntry("on-demand");
                    yield return new CompletionListEntry("hover");
                    break;

                case "light-level":
                    yield return new CompletionListEntry("dim");
                    yield return new CompletionListEntry("normal");
                    yield return new CompletionListEntry("washed");
                    break;

                case "scripting":
                    yield return new CompletionListEntry("none");
                    yield return new CompletionListEntry("initial-only");
                    yield return new CompletionListEntry("enabled");
                    break;

                case "width":
                case "max-width":
                case "min-width":
                case "device-width":
                case "max-device-width":
                case "min-device-width":
                    foreach (var browser in BrowserLink.BrowserInfo.BrowserCapDictionary.Values.OrderByDescending(b => b.Width))
                    {
                        string value = browser.Width + "px";
                        yield return new BrowserCompletionListEntry(value, browser.Name);
                    }
                    break;

                case "height":
                case "max-height":
                case "min-height":
                case "device-height":
                case "max-device-height":
                case "min-device-height":
                    foreach (var browser in BrowserLink.BrowserInfo.BrowserCapDictionary.Values.OrderByDescending(b => b.Height))
                    {
                        string value = browser.Height + "px";
                        yield return new BrowserCompletionListEntry(value, browser.Name);
                    }
                    break;
            }
        }
    }
}
