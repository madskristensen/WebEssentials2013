using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [HtmlCompletionProvider(CompletionType.Values, "*", "class")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class BootstrapLogoCompletion : IHtmlCompletionListProvider
    {
        private static BitmapFrame _icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/bootstrap.png", UriKind.RelativeOrAbsolute));
        private static List<string> _classes = new List<string>()
        {
            "active",
            "affix",
            "alert",
            "arrow",
            "badge",
            "bg-",
            "blockquote-reverse",
            "bottom",
            "breadcrumb",
            "btn",
            "caption",
            "caret",
            "carousel",
            "center-block",
            "checkbox",
            "clearfix",
            "close",
            "col",
            "collapse",
            "collapsing",
            "container",
            "control-label",
            "danger",
            "disabled",
            "divider",
            "dl-horizontal",
            "dropdown",
            "dropup",
            "embed-",
            "fade",
            "form-",
            "glyphicon",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "has-",
            "help-block",
            "hidden",
            "hide",
            "hover-controls",
            "icon",
            "img",
            "in",
            "info",
            "initialism",
            "input",
            "invisible",
            "ir",
            "item",
            "jumbotron",
            "label",
            "lead",
            "left",
            "list",
            "mark",
            "media",
            "modal",
            "nav",
            "navbar",
            "next",
            "open",
            "page-header",
            "pager",
            "pagination",
            "panel",
            "pill",
            "popover",
            "pre-scrollable",
            "prettyprint",
            "prev",
            "previous",
            "progress",
            "pull",
            "radio",
            "right",
            "row",
            "show",
            "small",
            "sr-",
            "success",
            "tab",
            "tabbable",
            "table",
            "text",
            "thumbnail",
            "tooltip",
            "top",
            "visible",
            "warning",
            "well"
        };

        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (context == null || context.Session == null || context.Session.CompletionSets == null || context.Session.CompletionSets.Count == 0)
                    return;

                var completions = context.Session.CompletionSets[0].Completions;

                if (completions == null)
                    return;

                for (int i = completions.Count - 1; i >= 0; i--)
                {
                    var item = completions[i] as HtmlCompletion;

                    if (item != null && IsMatch(item.DisplayText))
                    {
                        //if (!IsAllowed(item.DisplayText, context.Element))
                        //    completions.RemoveAt(i);
                        //else
                        item.IconSource = _icon;
                    }
                }

            }), DispatcherPriority.Normal, null);

            return new List<HtmlCompletion>();
        }

        private static bool IsMatch(string name)
        {
            if (_classes.Contains(name))
                return true;

            if (name.Contains('-'))
            {
                string first = name.Substring(0, name.IndexOf('-'));
                if (_classes.Contains(first) || _classes.Contains(first + "-"))
                    return true;
            }

            return false;
        }
    }
}
