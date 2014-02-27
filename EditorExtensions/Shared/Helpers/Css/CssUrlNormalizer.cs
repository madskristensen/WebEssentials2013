using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.Helpers
{
    class CssUrlNormalizer : ICssSimpleTreeVisitor
    {
        readonly string targetFile, oldBaseDirectory;
        readonly List<Tuple<TextRange, string>> replacements = new List<Tuple<TextRange, string>>();
        private CssUrlNormalizer(string targetFile, string oldBasePath)
        {
            this.targetFile = Path.GetFullPath(targetFile);
            this.oldBaseDirectory = Path.GetDirectoryName(oldBasePath);
        }

        ///<summary>Normalizes all URLs in a CSS parse tree to be relative to the specified directory.</summary>
        ///<param name="tree">The CSS parse tree to read.</param>
        ///<param name="targetFile">The new filename to make URLs relative to.</param>
        ///<param name="oldBasePath">The previous filename to resolve existing relative paths from.  If null, relative paths will be left unchanged.</param>
        ///<returns>The rewritten CSS source.</returns>
        ///<remarks>
        /// This is used to combine CSS files from different folders into a single
        /// bundle and to fix absolute URLs from the hacked browser LESS compiler.
        /// Fully absolute paths (starting with a drive letter) will be changed to
        /// relative URLs using targetFile (fix for LESS compiler).
        /// Host-relative paths (starting with a /) will be unchanged.
        /// Normal relative paths (starting with . or a name) will be re-resolved.
        ///</remarks>
        public static string NormalizeUrls(BlockItem tree, string targetFile, string oldBasePath)
        {
            var normalizer = new CssUrlNormalizer(targetFile, oldBasePath);
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

            var newUrl = FixPath(DecodeStringLiteral(urlItem.UrlString.Text));
            if (newUrl == null) // No change
                return VisitItemResult.Continue;

            replacements.Add(Tuple.Create(urlItem.UrlString.Range, newUrl));

            return VisitItemResult.Continue;
        }

        private static string DecodeStringLiteral(string str)
        {
            if ((str.StartsWith("'", StringComparison.Ordinal) && str.EndsWith("'", StringComparison.Ordinal))
             || (str.StartsWith("\"", StringComparison.Ordinal) && str.EndsWith("\"", StringComparison.Ordinal)))
                return str.Substring(1, str.Length - 2);
            return Regex.Unescape(str);
        }

        static readonly Regex urlRegex = new Regex(@"^[a-z]{2,}:");
        private string FixPath(string url)
        {
            // Ignore absolute URLs, whether domain-relative, protocol-relative, or fully absolute.  (as opposed to Windows paths with drive letters)
            if (url.StartsWith("/", StringComparison.Ordinal) || urlRegex.IsMatch(url))
                return null;

            string suffix = "";
            int breakIndex = url.IndexOfAny(new[] { '?', '#' });
            if (breakIndex > 0)
            {
                suffix = url.Substring(breakIndex);
                url = url.Remove(breakIndex);
            }

            if (Path.IsPathRooted(url))
                return FileHelpers.RelativePath(targetFile, Path.GetFullPath(url)) + suffix;

            if (string.IsNullOrEmpty(oldBaseDirectory))
                return null;

            return FileHelpers.RelativePath(
                targetFile,
                Path.GetFullPath(Path.Combine(oldBaseDirectory, url)) + suffix
            );
        }
    }
}
