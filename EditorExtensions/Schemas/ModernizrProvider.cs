using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSelectorFormatterHierarchyFilter))]
    [Name("ModernizrProvider")]
    internal class ModernizrProvider : ICssSelectorFormatterHierarchyFilter
    {
        private int _offset = 2;
        public static string[] _classes = new string[] { "js", "flexbox", "flexbox-legacy", "canvas", "canvastext", "webgl", "touch", "geolocation", "postmessage", "websqldatabase", "indexeddb", "hashchange", "history", "draganddrop", "websockets", "rgba", "hsla", "multiplebgs", "backgroundsize", "borderimage", "borderradius", "boxshadow", "textshadow", "opacity", "cssanimations", "csscolumns", "cssgradients", "cssreflections", "csstransforms", "csstransforms3d", "csstransitions", "fontface", "generatedcontent", "video", "audio", "localstorage", "sessionstorage", "webworkers", "applicationcache", "svg", "inlinesvg", "smil", "svgclippaths" };

        public bool FilterSelectorTokens(IList<TokenItem> tokens)
        {
            if (tokens.Count > _offset)
            {
                return IsModernizr(tokens[0].Text + tokens[1].Text);
            }

            return false;
        }

        public bool IsSelectorIndented(IList<TokenItem> parentTokens, IList<TokenItem> tokens)
        {
            if (tokens.Count <= _offset || parentTokens.Count == tokens.Count)
                return false;

            bool isChildModernizr = IsModernizr(tokens[0].Text + tokens[1].Text);

            if (!isChildModernizr)
                return false;

            bool isParentModernizr = parentTokens.Count >= 2 ? IsModernizr(parentTokens[0].Text + parentTokens[1].Text) : false;

            if (isParentModernizr && parentTokens.Count >= tokens.Count)
                return false;

            int start = isParentModernizr ? _offset : 0;

            for (int i = 0; i < parentTokens.Count - start; i++)
            {
                string parent = parentTokens[i + start].Text;
                string child = tokens[i + _offset].Text;

                if (parent != child)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsModernizr(string text)
        {
            if (!text.StartsWith(".", StringComparison.Ordinal))
                return false;

            string name = text.ToLowerInvariant().TrimStart('.');

            if (name.StartsWith("no-", StringComparison.Ordinal))
            {
                name = name.Substring(3);
            }

            return _classes.Contains(name);
        }
    }
}
