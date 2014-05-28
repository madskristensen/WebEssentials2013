using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
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

                case "width":    
                case "max-width":
                case "min-width":
                case "device-width":
                case "max-device-width":
                case "min-device-width":
                    foreach (var browser in BrowserLink.BrowserInfo._infos.Values.OrderByDescending(b => b.Width))
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
                    foreach (var browser in BrowserLink.BrowserInfo._infos.Values.OrderByDescending(b => b.Height))
                    {
                        string value = browser.Height + "px";
                        yield return new BrowserCompletionListEntry(value, browser.Name);
                    }
                    break;
            }
        }
    }
}
