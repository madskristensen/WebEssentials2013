using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Scss.Core;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>Shared class between Base64Vlq and CssSourceMap</summary>
    public class CssSourceMapNode : SourceMapNode
    {
        public Selector GeneratedSelector { get; set; }
        public Selector OriginalSelector { get; set; }
    }

    ///<summary>CSS source map factory.</summary>
    ///<remarks>
    /// The objects of this class will be instantiated
    /// when LESS or SCSS document is loaded in VS,
    /// and finally get stored in DependencyGraph.
    ///</remarks>
    public sealed class CssSourceMap
    {
        private string _directory;
        private ICssParser _parser;
        private IContentType _contentType;

        public IEnumerable<CssSourceMapNode> MapNodes { get; private set; }

        private CssSourceMap()
        { }

        public async static Task<CssSourceMap> Create(string targetFileContents, string mapFileContents, string directory, IContentType contentType)
        {
            CssSourceMap map = new CssSourceMap();

            map.MapNodes = Enumerable.Empty<CssSourceMapNode>();
            var settings = WESettings.Instance.ForContentType<ICssSourceMapSettings>(contentType);

            if (settings != null && settings.ProcessSourceMapsForEditorEnhancements)
                await Task.Run(() => map.Initialize(targetFileContents, mapFileContents, directory, contentType));

            return map;
        }

        private void Initialize(string targetFileContents, string mapFileContents, string directory, IContentType contentType)
        {
            _contentType = contentType;
            _parser = CssParserLocator.FindComponent(_contentType).CreateParser();
            _directory = directory;
            PopulateMap(targetFileContents, mapFileContents); // Begin two-steps initialization.
        }

        private void PopulateMap(string targetFileContents, string mapFileContents)
        {
            SourceMapDefinition map = null;

            try
            {
                map = JsonConvert.DeserializeObject<SourceMapDefinition>(mapFileContents);
            }
            catch { }

            if (map == null)
                return;

            try
            {
                MapNodes = Base64Vlq.Decode(map.mappings, _directory, map.sources).ToList();


                if (MapNodes.Count() == 0)
                    return;
            }
            catch (VlqException vlqExcept)
            {
                Logger.Log("VLQ: " + vlqExcept.Message);
                return;
            }

            CollectRules(targetFileContents).DoNotWait("collecting maps");
        }

        private async Task CollectRules(string targetFileContents)
        {
            // Sort collection for source file.
            MapNodes = MapNodes.OrderBy(x => x.SourceFilePath)
                               .ThenBy(x => x.OriginalLine)
                               .ThenBy(x => x.OriginalColumn);

            MapNodes = await ProcessSourceMaps();

            // Sort collection for generated file.
            MapNodes = MapNodes.OrderBy(x => x.GeneratedLine)
                               .ThenBy(x => x.GeneratedColumn);

            MapNodes = ProcessGeneratedMaps(targetFileContents);
        }

        private async Task<IEnumerable<CssSourceMapNode>> ProcessSourceMaps()
        {
            ParseItem item = null;
            Selector selector = null;
            StyleSheet styleSheet = null;
            int start;
            var fileContents = "";
            var result = new List<CssSourceMapNode>();
            var contentCollection = new HashSet<string>();

            foreach (var node in MapNodes)
            {
                // Cache source file contents.
                if (!contentCollection.Contains(node.SourceFilePath))
                {
                    if (!File.Exists(node.SourceFilePath)) // Lets say someone deleted the reference file.
                        continue;

                    fileContents = await FileHelpers.ReadAllTextRetry(node.SourceFilePath);

                    contentCollection.Add(node.SourceFilePath);

                    styleSheet = _parser.Parse(fileContents, false);
                }

                start = fileContents.NthIndexOfCharInString('\n', node.OriginalLine);
                start += node.OriginalColumn;

                item = styleSheet.ItemAfterPosition(start);

                if (item == null)
                    continue;

                selector = item.FindType<Selector>();

                if (selector == null)
                    continue;

                node.OriginalSelector = selector;

                result.Add(node);
            }

            return result;
        }

        private IEnumerable<CssSourceMapNode> ProcessGeneratedMaps(string cssfileContents)
        {
            ParseItem item = null;
            Selector selector = null;
            StyleSheet styleSheet = null;
            SimpleSelector simple = null;
            int start;
            var parser = new CssParser();
            var result = new List<CssSourceMapNode>();

            styleSheet = parser.Parse(cssfileContents, false);

            foreach (var node in MapNodes)
            {
                start = cssfileContents.NthIndexOfCharInString('\n', node.GeneratedLine);
                start += node.GeneratedColumn;

                item = styleSheet.ItemAfterPosition(start);

                if (item == null)
                    continue;

                selector = item.FindType<Selector>();

                if (selector == null)
                    continue;

                var depth = node.OriginalSelector.TreeDepth / 2;

                if (depth < selector.SimpleSelectors.Count)
                {
                    simple = selector.SimpleSelectors.First();

                    if (simple == null)
                        simple = item.Parent != null ? item.Parent.FindType<SimpleSelector>() : null;

                    if (simple == null)
                        continue;

                    var selectorText = new StringBuilder();

                    for (int i = 0; i < node.OriginalSelector.SimpleSelectors.Count; i++)
                    {
                        if (simple == null)
                            break;

                        selectorText.Append(simple.Text).Append(" ");

                        simple = simple.NextSibling as SimpleSelector;
                    }

                    StyleSheet sheet = parser.Parse(selectorText.ToString(), false);

                    if (sheet == null || !sheet.RuleSets.Any() || !sheet.RuleSets.First().Selectors.Any())
                        continue;

                    selector = sheet.RuleSets.First().Selectors.First() as Selector;

                    if (selector == null)
                        continue;
                }

                node.GeneratedSelector = selector;

                result.Add(node);
            }

            return result;
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class SourceMapDefinition
        {
            public string file { get; set; }
            public string mappings { get; set; }
            public string[] sources { get; set; }
        }
    }
}