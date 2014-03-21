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
        public RuleSet GeneratedItem { get; set; }
        public RuleSet OriginalItem { get; set; }
    }

    ///<summary>CSS source map factory.</summary>
    ///<remarks>
    /// The objects of this class will be instantiated
    /// when LESS or SCSS document is loaded in VS,
    /// and finally get stored in DependencyGraph.
    ///</remarks>
    public class CssSourceMap
    {
        private string _directory;
        private ICssParser _parser;

        public IEnumerable<CssSourceMapNode> MapNodes { get; private set; }
        public bool IsDirty { get; private set; }

        public CssSourceMap(string sourceFileName, string targetFileName, string mapFileName, IContentType contentType)
        {
            _parser = CssParserLocator.FindComponent(contentType).CreateParser();
            _directory = Path.GetDirectoryName(sourceFileName);
            IsDirty = PopulateMap(targetFileName, mapFileName); // Begin two-steps initialization.
        }

        private bool PopulateMap(string targetFileName, string mapFileName)
        {
            V3SourceMap map = new V3SourceMap();

            try
            {
                map = Json.Decode<V3SourceMap>(File.ReadAllText(mapFileName));
            }
            catch
            {
                return false;
            }

            MapNodes = Base64Vlq.Decode(map.mappings, _directory, map.sources);

            if (MapNodes.Count() == 0)
                return false;

            return CollectRules(targetFileName);
        }

        private bool CollectRules(string targetFileName)
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

            return MapNodes.Any();
        }

        private IEnumerable<CssSourceMapNode> ProcessCollection(string fileContents = null)
        {
            RuleSet rule = null;
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

                    // Cache file contents for LESS/SCSS.
                    if (!contentCollection.ContainsKey(node.SourceFilePath))
                    {
                        fileContents = File.ReadAllText(node.SourceFilePath);

                        contentCollection.Add(node.SourceFilePath, fileContents);

                        styleSheet = _parser.Parse(fileContents, false);
                    }
                }

                start = fileContents.NthIndexOfCharInString('\n', line - 1);
                start += column;

                item = styleSheet.ItemAfterPosition(start);
                rule = item.FindType<RuleSet>();

                if (rule == null)
                    continue;

                if (isSource)
                    node.OriginalItem = rule;
                else
                    node.GeneratedItem = rule;

                result.Add(node);
            }

            return result;
        }

        private struct V3SourceMap
        {
            public string mappings;
            public string[] sources;
        }
    }
}