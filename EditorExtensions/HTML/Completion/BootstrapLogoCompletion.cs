using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Html.Core;
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
                if (context.Session.CompletionSets.Count == 0)
                    return;

                var completions = context.Session.CompletionSets[0].Completions;

                for (int i = completions.Count - 1; i >= 0; i--)
                {
                    var item = completions[i] as HtmlCompletion;

                    if (IsMatch(item.DisplayText))
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

        private static List<string> _inputElmts = new List<string> { "input", "select", "textarea" };
        private static List<string> _btnElmts = new List<string> { "input", "button", "a" };
        private static List<string> _btnNames = new List<string> { "btn", "btn-primary", "btn-default", "btn-success", "btn-info", "btn-warning", "btn-danger", "btn-link", "btn-lg", "btn-sm", "btn-xs", "btn-block", "", "", "" };

        private static bool IsAllowed(string name, ElementNode element)
        {
            if (name.StartsWith("glyphicon") && element.Name != "span")
                return false;

            if (name.StartsWith("table") && !name.StartsWith("table-responsive") && element.Name != "table")
                return false;

            if (name == "form-control" && !_inputElmts.Contains(element.Name))
                return false;

            if (name == "control-label" && element.Name != "label")
                return false;

            if (_btnNames.Contains(name) && _btnElmts.Contains(element.Name))
                return false;

            if (name.StartsWith("img") && element.Name != "img")
                return false;

            if ((name.StartsWith("pagination") || name.StartsWith("pager")) && element.Name != "ul")
                return false;

            if (name.StartsWith("dl-horizontal") && element.Name != "dl")
                return false;

            return true;
        }
    }
}
