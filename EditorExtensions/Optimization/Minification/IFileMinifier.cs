using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
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
        ///<returns>True if the minified file has changed from the old version on disk.</returns>
        bool MinifyFile(string sourcePath, string targetPath);

        ///<summary>Minifies a string in-memory.</summary>
        string MinifyString(string source);

        ///<summary>Indicates whether the minifier will emit a source map file next to the minified output.</summary>
        bool GenerateSourceMap { get; }
    }

    ///<summary>An <see cref="IFileMinifier"/> that minifies files in-memory, then writes the results to disk.</summary>
    public abstract class InMemoryMinifier : IFileMinifier
    {
        public virtual bool GenerateSourceMap { get { return false; } }

        public virtual bool MinifyFile(string sourcePath, string targetPath)
        {
            var result = MinifyString(File.ReadAllText(sourcePath));

            if (result != null && (!File.Exists(targetPath) || result != File.ReadAllText(targetPath)))
            {
                ProjectHelpers.CheckOutFileFromSourceControl(targetPath);
                File.WriteAllText(targetPath, result, Encoding.UTF8);
                ProjectHelpers.AddFileToProject(sourcePath, targetPath);

                return true;
            }

            return false;
        }
        public abstract string MinifyString(string source);
    }

    [Export(typeof(IFileMinifier))]
    [ContentType("HTMLX")]
    public class HtmlFileMinifier : InMemoryMinifier
    {
        public override string MinifyString(string source)
        {
            var settings = new HtmlMinificationSettings
            {
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
    public class CssFileMinifier : InMemoryMinifier
    {
        public override string MinifyString(string source)
        {
            Minifier minifier = new Minifier();

            var settings = new Microsoft.Ajax.Utilities.CssSettings
            {
                CommentMode = WESettings.Instance.General.KeepImportantComments ? CssComment.Hacks : CssComment.Important
            };

            return minifier.MinifyStyleSheet(source, settings);
        }
    }

    [Export(typeof(IFileMinifier))]
    [ContentType("JavaScript")]
    public class JavaScriptFileMinifier : InMemoryMinifier
    {
        public override bool GenerateSourceMap { get { return WESettings.Instance.JavaScript.GenerateSourceMaps; } }

        static CodeSettings CreateSettings()
        {
            return new CodeSettings()
            {
                EvalTreatment = EvalTreatment.MakeImmediateSafe,
                TermSemicolons = true,
                PreserveImportantComments = WESettings.Instance.General.KeepImportantComments
            };
        }

        public override string MinifyString(string source)
        {
            return new Minifier().MinifyJavaScript(source, CreateSettings());
        }

        public override bool MinifyFile(string sourcePath, string targetPath)
        {
            if (GenerateSourceMap)
                return MinifyFileWithSourceMap(sourcePath, targetPath);
            else
                return base.MinifyFile(sourcePath, targetPath);
        }

        private static bool MinifyFileWithSourceMap(string file, string minFile)
        {
            string mapPath = minFile + ".map";
            ProjectHelpers.CheckOutFileFromSourceControl(mapPath);

            using (TextWriter writer = new StreamWriter(mapPath, false, new UTF8Encoding(false)))
            using (V3SourceMap sourceMap = new V3SourceMap(writer))
            {
                var settings = CreateSettings();
                settings.SymbolsMap = sourceMap;
                sourceMap.StartPackage(Path.GetFileName(minFile), Path.GetFileName(mapPath));

                // This fails when debugger is attached. Bug raised with Ron Logan
                bool result = MinifyFile(file, minFile, settings);
                ProjectHelpers.AddFileToProject(minFile, mapPath);

                return result;
            }
        }

        private static bool MinifyFile(string file, string minFile, CodeSettings settings)
        {
            Minifier minifier = new Minifier();

            // If the source file is not itself mapped, add the filename for mapping
            // TODO: Make sure this works for compiled output too. (check for .map?)
            if (!(File.ReadLines(file)
                      .SkipWhile(string.IsNullOrWhiteSpace)
                      .FirstOrDefault() ?? "")
                      .Trim()
                      .StartsWith("///#source", StringComparison.CurrentCulture))
            {
                minifier.FileName = Path.GetFileName(file);
            }

            string content = minifier.MinifyJavaScript(File.ReadAllText(file), settings);
            content += "\r\n/*\r\n//# sourceMappingURL=" + Path.GetFileName(minFile) + ".map\r\n*/";

            if (File.Exists(minFile) && content == File.ReadAllText(minFile))
                return false;

            ProjectHelpers.CheckOutFileFromSourceControl(minFile);
            File.WriteAllText(minFile, content, Encoding.UTF8);
            ProjectHelpers.AddFileToProject(file, minFile);

            return true;
        }
    }
}