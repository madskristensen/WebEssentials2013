using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.Helpers
{
    class CssUrlNormalizer : ICssSimpleTreeVisitor
    {
        readonly string baseDirectory, oldBasePath;
        readonly List<Tuple<TextRange, string>> replacements = new List<Tuple<TextRange, string>>();
        private CssUrlNormalizer(string baseDirectory, string oldBasePath)
        {
            this.baseDirectory = baseDirectory;
            this.oldBasePath = oldBasePath;
        }

        ///<summary>Normalizes all URLs in a CSS parse tree to be relative to the specified directory.</summary>
        ///<param name="tree">The CSS parse tree to read.</param>
        ///<param name="baseDirectory">The new base directory to make URLs relative to.</param>
        ///<param name="oldBasePath">The previous base directory to resolve existing relative paths from.  If null, relative paths will be left unchanged.</param>
        ///<returns>The rewritten CSS source.</returns>
        ///<remarks>
        /// This is used to combine CSS files from different folders into a single
        /// bundle and to fix absolute URLs from the hacked browser LESS compiler.
        /// Fully absolute paths (starting with a drive letter) will be changed to
        /// relative URLs using baseDirectory (fix for LESS compiler).
        /// Host-relative paths (starting with a /) will be unchanged.
        /// Normal relative paths (starting with . or a name) will be re-resolved.
        ///</remarks>
        public static string NormalizeUrls(BlockItem tree, string baseDirectory, string oldBasePath)
        {
            var normalizer = new CssUrlNormalizer(baseDirectory, oldBasePath);
            tree.Accept(normalizer);

            var retVal = new StringBuilder(tree.Text);
            for (int i = normalizer.replacements.Count - 1; i >= 0; i--)
            {
                var range = normalizer.replacements[i].Item1;
                var url = normalizer.replacements[i].Item2;

                retVal.Remove(range.Start, range.Length);
                retVal.Insert(range.Start, HttpUtility.UrlPathEncode(url));
            }
            return retVal.ToString();
        }

        public VisitItemResult Visit(ParseItem parseItem)
        {
            var urlItem = parseItem as UrlItem;
            if (urlItem == null)
                return VisitItemResult.Continue;

            var newUrl = FixPath(urlItem.UrlString.Text);
            if (newUrl == null) // No change
                return VisitItemResult.Continue;

            replacements.Add(Tuple.Create(urlItem.UrlString.Range, newUrl));

            return VisitItemResult.Continue;
        }

        private string FixPath(string url)
        {
            if (url.StartsWith("/"))
                return null;
            if (Path.IsPathRooted(url))
                return FileHelpers.RelativePath(url, baseDirectory);

            if (string.IsNullOrEmpty(oldBasePath))
                return null;

            return FileHelpers.RelativePath(Path.Combine(oldBasePath, url), baseDirectory);
        }
    }
}
