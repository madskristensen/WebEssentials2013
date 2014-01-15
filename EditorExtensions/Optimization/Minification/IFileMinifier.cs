using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;
using Microsoft.VisualStudio.Utilities;
using WebMarkupMin.Core;
using WebMarkupMin.Core.Minifiers;
using WebMarkupMin.Core.Settings;

namespace MadsKristensen.EditorExtensions.Optimization.Minification
{
    public interface IFileMinifier
    {
        ///<summary>Minifies an existing file, creating or overwriting the target.</summary>
        void MinifyFile(string sourcePath, string targetPath);

        ///<summary>Minifies a string in-memory.</summary>
        string MinifyString(string source);
    }

    ///<summary>An <see cref="IFileMinifier"/> that minifies files in-memory, then writes the results to disk.</summary>
    public abstract class InMemoryMinifier : IFileMinifier
    {
        public void MinifyFile(string sourcePath, string targetPath)
        {
            var result = MinifyString(File.ReadAllText(sourcePath));
            if (result != null)
                File.WriteAllText(targetPath, result);
        }
        public abstract string MinifyString(string source);
    }

    [Export(typeof(IFileMinifier))]
    [ContentType("HTMLX")]
    public class HtmlFileMinifier : InMemoryMinifier
    {
        public override string MinifyString(string source)
        {
            var settings = new HtmlMinificationSettings {
                RemoveOptionalEndTags = false,
                AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes,
                RemoveRedundantAttributes = false,
            };

            var minifier = new HtmlMinifier(settings);
            MarkupMinificationResult result = minifier.Minify(source, generateStatistics: true);
            if (result.Errors.Count == 0)
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: HTML minified by " + result.Statistics.SavedInPercent + "%";
                return result.MinifiedContent;
            }
            else
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Cannot minify the current selection.  See Output Window for details.";
                Logger.ShowMessage("Cannot minify the selection:\r\n\r\n" + String.Join(Environment.NewLine, result.Errors.Select(e => e.Message)));
                return null;
            }
        }
    }

    [Export(typeof(IFileMinifier))]
    [ContentType("CSS")]
    public class CssFileMinifer : InMemoryMinifier
    {
        public override string MinifyString(string source)
        {
            Minifier minifier = new Minifier();
            var settings = new Microsoft.Ajax.Utilities.CssSettings {
                CommentMode = WESettings.Instance.General.KeepImportantComments ? CssComment.Hacks : CssComment.Important
            };

            return minifier.MinifyStyleSheet(source, settings);
        }
    }

    [Export(typeof(IFileMinifier))]
    [ContentType("JavaScript")]
    public class JavaScriptFileMinifer : InMemoryMinifier
    {
        public override string MinifyString(string source)
        {
            Minifier minifier = new Minifier();
            CodeSettings settings = new CodeSettings() {
                EvalTreatment = EvalTreatment.MakeImmediateSafe,
                PreserveImportantComments = WESettings.Instance.General.KeepImportantComments
            };

            return minifier.MinifyJavaScript(source, settings);
        }
    }
}