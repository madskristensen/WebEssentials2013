using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(IColorPickerSwatchProvider))]
    [Name("XmlColorPaletteProvider")]
    [Order(Before = "Default")]
    internal class XmlColorPaletteProvider : IColorPickerSwatchProvider
    {
        private const string _fileName = "WE-Palette.css";
        private const string _rootDirective = "-we-palette";
        private static bool _hasFile = true;
        private static FileSystemWatcher _watcher;
        private static IList<AtDirective> _directives;

        public static bool SolutionColorsExist
        {
            get
            {
                return File.Exists(GetSolutionFilePath());
            }

        }

        public static void CreateSolutionColors()
        {
            if (SolutionColorsExist)
                return;

            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly);
            string sourcePath = Path.Combine(folder, "css\\schemas\\WE-Palette.css");
            string targetPath = GetSolutionFilePath();

            File.Copy(sourcePath, targetPath, true);
            ProjectHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(targetPath);
            WebEssentialsPackage.DTE.ItemOperations.OpenFile(targetPath);
        }

        public IEnumerable<ColorModel> GetColors(ITextView textView, SnapshotSpan contextSpan)
        {
            if (_directives == null && _hasFile)
                ParseDocument().Wait();

            if (_directives != null && textView != null)
            {
                CssEditorDocument document = CssEditorDocument.FromTextBuffer(textView.TextBuffer);
                ParseItem item = document.Tree.StyleSheet.ItemAfterPosition(contextSpan.Start);
                Declaration declaration = item.FindType<Declaration>();

                if (declaration != null)
                {
                    return GetApplicableColors();
                }
            }

            return new List<ColorModel>();
        }

        private async static Task ParseDocument()
        {
            string fileName = GetSolutionFilePath();

            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                CssParser parser = new CssParser();
                StyleSheet stylesheet = parser.Parse(await FileHelpers.ReadAllTextRetry(fileName), false);

                if (stylesheet.IsValid)
                {
                    var visitor = new CssItemCollector<AtDirective>();
                    stylesheet.Accept(visitor);

                    AtDirective at = visitor.Items.SingleOrDefault(a => a.Keyword.Text == _rootDirective);
                    if (at != null)
                    {
                        var visitorPalette = new CssItemCollector<AtDirective>(true);
                        at.Accept(visitorPalette);
                        _directives = visitorPalette.Items.Where(a => a.Keyword.Text != at.Keyword.Text).ToList();
                    }
                }

                InitializeWatcher(fileName);
            }
            else
            {
                _hasFile = false;
            }
        }

        private static IEnumerable<ColorModel> GetApplicableColors()
        {
            List<ParseItem> items = new List<ParseItem>();
            //bool hasCustomItems = false;

            foreach (var item in _directives.Where(d => d.IsValid))
            {
                // Global
                if (item.Keyword.Text == "global")
                {
                    var visitor = new CssItemCollector<Declaration>();
                    item.Accept(visitor);
                    items.AddRange(visitor.Items);
                }
                // Specific
                //else if (item.Keyword.Text == "specific")
                //{
                //    var visitor = new CssItemCollector<Declaration>();
                //    item.Accept(visitor);
                //    var decs = visitor.Items.Where(d => propertyName.StartsWith(d.PropertyName.Text));
                //    if (decs.Any())
                //    {
                //        items.AddRange(decs);
                //        hasCustomItems = true;
                //    }
                //}
                //// Catch all
                //else if (!hasCustomItems && item.Keyword.Text == "unspecified")
                //{
                //    var visitor = new CssItemCollector<Declaration>();
                //    item.Accept(visitor);
                //    items.AddRange(visitor.Items);
                //}
            }

            foreach (Declaration declaration in items)
            {
                ColorModel model = ColorParser.TryParseColor(declaration.Values[0].Text, ColorParser.Options.AllowAlpha | ColorParser.Options.AllowNames);
                if (model != null)
                {
                    yield return model;
                }
            }
        }

        public static string GetSolutionFilePath()
        {
            EnvDTE.Solution solution = WebEssentialsPackage.DTE.Solution;

            if (solution == null || string.IsNullOrEmpty(solution.FullName))
                return null;

            return Path.Combine(Path.GetDirectoryName(solution.FullName), _fileName);
        }

        private static void InitializeWatcher(string fileName)
        {
            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher();
                _watcher.Path = Path.GetDirectoryName(fileName);
                _watcher.Filter = _fileName;
                _watcher.EnableRaisingEvents = true;
                _watcher.Created += delegate { _hasFile = true; };
                _watcher.Changed += delegate { _directives = null; };
                _watcher.Deleted += delegate { _directives = null; _hasFile = false; };
                _watcher.Renamed += delegate { _directives = null; _hasFile = File.Exists(fileName); };
            }
        }

        public bool AllowProvider(string providerName)
        {
            if (_directives == null)
                ParseDocument().Wait();

            if (_directives != null)
                return providerName != "Default" && providerName != "Document Colors";

            return true;
        }
    }
}
