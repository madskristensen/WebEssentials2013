using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("UnknownTagErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class UnknownTagErrorTagProvider : ICssItemChecker
    {
        private HashSet<string> _cache = new HashSet<string>(){
            "*",
            "a",
            "abbr",
            "acronym",
            "address",
            "applet",
            "area",
            "article",
            "aside",
            "audio",
            "b",
            "base",
            "basefont",
            "bdi",
            "bdo",
            "big",
            "blockquote",
            "body",
            "br",
            "button",
            "canvas",
            "caption",
            "center",
            "cite",
            "code",
            "col",
            "colgroup",
            "command",
            "datalist",
            "dd",
            "del",
            "details",
            "dfn",
            "dir",
            "div",
            "dl",
            "dt",
            "em",
            "embed",
            "fieldset",
            "figcaption",
            "figure",
            "font",
            "footer",
            "form",
            "frame",
            "frameset",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "head",
            "header",
            "hgroup",
            "hr",
            "html",
            "i",
            "iframe",
            "img",
            "input",
            "ins",
            "keygen",
            "kbd",
            "label",
            "legend",
            "li",
            "link",
            "map",
            "mark",
            "menu",
            "meta",
            "meter",
            "nav",
            "noframes",
            "noscript",
            "object",
            "ol",
            "optgroup",
            "option",
            "output",
            "p",
            "param",
            "pre",
            "progress",
            "q",
            "rp",
            "rt",
            "ruby",
            "s",
            "samp",
            "script",
            "section",
            "select",
            "small",
            "source",
            "span",
            "strike",
            "strong",
            "style",
            "sub",
            "summary",
            "sup",
            "svg",
            "table",
            "tbody",
            "td",
            "textarea",
            "tfoot",
            "th",
            "thead",
            "time",
            "title",
            "tr",
            "track",
            "tt",
            "u",
            "ul",
            "var",
            "video",
            "wbr"
        };

        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            ItemName itemName = (ItemName)item;

            if (!itemName.IsValid || context == null || (item.PreviousSibling != null && item.PreviousSibling.Text == "["))
                return ItemCheckResult.Continue;

            if (!_cache.Contains(itemName.Text.ToLowerInvariant()))
            {
                string error = "Validation: \"" + itemName.Text + "\" isn't a valid HTML tag.";
                ICssError tag = new SimpleErrorTag(itemName, error);
                context.AddError(tag);

                return ItemCheckResult.CancelCurrentItem;
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(ItemName) }; }
        }
    }
}
