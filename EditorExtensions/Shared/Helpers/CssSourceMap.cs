using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Helpers;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;

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

        public IEnumerable<CssSourceMapNode> MapNodes { get; private set; }

        private CssSourceMap()
        { }

        public CssSourceMap(string targetFileName, string mapFileName, IContentType contentType)
        {
            Initialize(targetFileName, mapFileName, contentType);
        }

        private void Initialize(string targetFileName, string mapFileName, IContentType contentType)
        {
            _parser = CssParserLocator.FindComponent(contentType).CreateParser();
            _directory = Path.GetDirectoryName(mapFileName);
            PopulateMap(targetFileName, mapFileName); // Begin two-steps initialization.
        }

        private void PopulateMap(string targetFileName, string mapFileName)
        {
            var map = new SourceMapDefinition();

            try
            {
                map = Json.Decode<SourceMapDefinition>(File.ReadAllText(mapFileName));
            }
            catch
            {
                return;
            }

            MapNodes = Base64Vlq.Decode(map.mappings, _directory, map.sources);

            if (MapNodes.Count() == 0)
                return;

            CollectRules(targetFileName);
        }

        private void CollectRules(string targetFileName)
        {
            // Sort collection for generated file.
            MapNodes = MapNodes.OrderBy(x => x.GeneratedLine)
                               .ThenBy(x => x.GeneratedColumn);

            MapNodes = ProcessCollection(File.ReadAllText(targetFileName));

            // Sort collection for source file.
            MapNodes = MapNodes.OrderBy(x => x.SourceFilePath)
                               .ThenBy(x => x.OriginalLine)
                               .ThenBy(x => x.OriginalColumn);

            MapNodes = ProcessCollection();

            MapNodes.Any();
        }

        private IEnumerable<CssSourceMapNode> ProcessCollection(string fileContents = null)
        {
            Selector selector = null;
            ParseItem item = null;
            StyleSheet styleSheet = null;

            var result = new List<CssSourceMapNode>();
            var contentCollection = new Dictionary<string, string>(); // So we don't have to read file for each map item.

            int start = 0, column, line;
            bool isSource = true;

            if (fileContents != null) // Means its generated CSS file
            {
                styleSheet = new CssParser().Parse(fileContents, false);
                isSource = false;
            }

            foreach (var node in MapNodes)
            {
                if (!isSource)
                {
                    column = node.GeneratedColumn;
                    line = node.GeneratedLine;
                }
                else
                {
                    column = node.OriginalColumn;
                    line = node.OriginalLine;

                    if (node.SourceFilePath.EndsWith("scss", StringComparison.OrdinalIgnoreCase))
                        line--; // SCSS line count starts with 1

                    // Cache file contents for LESS/SCSS.
                    if (!contentCollection.ContainsKey(node.SourceFilePath))
                    {
                        if (!File.Exists(node.SourceFilePath))
                            continue;

                        fileContents = File.ReadAllText(node.SourceFilePath);

                        contentCollection.Add(node.SourceFilePath, fileContents);

                        styleSheet = _parser.Parse(fileContents, false);
                    }
                }

                start = fileContents.NthIndexOfCharInString('\n', line);
                start += column;

                item = styleSheet.ItemAfterPosition(start);

                selector = item.FindType<Selector>();

                if (selector == null)
                    continue;

                if (isSource)
                    node.OriginalSelector = selector;
                else
                    node.GeneratedSelector = selector;

                result.Add(node);
            }

            return result;
        }

        private struct SourceMapDefinition
        {
            public string file;
            public string mappings;
            public string[] sources;
        }
    }
}