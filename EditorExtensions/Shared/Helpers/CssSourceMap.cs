using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Threading;
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

        private static Dictionary<string, IEnumerable<CssSourceMapNode>> _sourceMaps = new Dictionary<string, IEnumerable<CssSourceMapNode>>();
        private static readonly AsyncReaderWriterLock _rwLock = new AsyncReaderWriterLock();

        public IEnumerable<CssSourceMapNode> MapNodes { get; private set; }
        public bool IsDirty { get; private set; }

        private CssSourceMap()
        { }

        public static dynamic GetSourcePosition(string targetFileName, int line, int column)
        {
            var node = GetSourceMapNode(targetFileName, line, column).Result;

            if (node == null)
                return null;

            return new
            {
                file = node.SourceFilePath,
                line = node.OriginalLine,
                column = node.OriginalColumn
            };
        }

        public static dynamic GetGeneratedPosition(string sourceFileName, string targetFileName, int line, int column)
        {
            var node = GetGeneratedMapNode(sourceFileName, targetFileName, line, column, string.IsNullOrEmpty(targetFileName)).Result;

            if (node == null)
                return null;

            return new
            {
                line = node.GeneratedLine,
                column = node.GeneratedColumn
            };
        }

        public static Selector GetSourceSelector(string targetFileName, int line, int column)
        {
            var node = GetSourceMapNode(targetFileName, line, column).Result;

            if (node == null)
                return null;

            return node.OriginalSelector;
        }

        public static Selector GetGeneratedSelector(string sourceFileName, string targetFileName, int line, int column)
        {
            var node = GetGeneratedMapNode(sourceFileName, targetFileName, line, column, string.IsNullOrEmpty(targetFileName)).Result;

            if (node == null)
                return null;

            return node.GeneratedSelector;
        }

        private async static Task<CssSourceMapNode> GetSourceMapNode(string targetFileName, int line, int column)
        {
            using (await _rwLock.ReadLockAsync())
            {
                return _sourceMaps.Where(s => s.Key == targetFileName)
                                  .SelectMany(s => s.Value)
                                  .FirstOrDefault(s => s.GeneratedColumn == column && s.GeneratedLine == line);
            }
        }

        private async static Task<CssSourceMapNode> GetGeneratedMapNode(string sourceFileName, string targetFileName, int line, int column, bool anyTarget)
        {
            if (sourceFileName.EndsWith("scss", StringComparison.OrdinalIgnoreCase))
                line--;

            using (await _rwLock.ReadLockAsync())
            {
                if (!anyTarget && !_sourceMaps.ContainsKey(targetFileName))
                    return null;

                var map = _sourceMaps.FirstOrDefault(s => (anyTarget || s.Key == targetFileName) && s.Value.Any(k => k.SourceFilePath == sourceFileName)).Value;

                if (map == null)
                    return null;

                var node = map.FirstOrDefault(n => n.OriginalLine == line && n.OriginalColumn == column);

                if (node != null)
                    return node;

                // In case of SCSS, white-spaces are not discounted.
                var nodeSet = map.Where(n => n.OriginalLine == line && n.OriginalColumn < column);

                if (nodeSet.Count() == 0)
                    return null;

                return nodeSet.OrderBy(o => o.OriginalColumn).Last();
            }
        }

        public static void GenerateMaps(string mapFileName)
        {
            var json = Json.Decode<SourceMapDefinition>(File.ReadAllText(mapFileName));
            string extension = Path.GetExtension(json.sources[0]);

            if (!extension.Equals(".less", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".scss", StringComparison.OrdinalIgnoreCase))
                return;

            var source = json.sources.Where(s => Path.GetFileNameWithoutExtension(s) == Path.GetFileName(mapFileName.Substring(0, mapFileName.IndexOf(".", StringComparison.Ordinal))));

            if (!source.Any())
                return;

            var directory = Path.GetDirectoryName(mapFileName);
            var sourceName = Path.GetFullPath(Path.Combine(directory, source.First()));
            string targetName = Path.GetFullPath(Path.Combine(directory, json.file));

            if (!File.Exists(sourceName) || !File.Exists(targetName))
                return;

            var contentType = Mef.GetContentType(extension.TrimStart('.'));

            GenerateMaps(sourceName, targetName, mapFileName, contentType);
        }

        public static void GenerateMaps(string sourceFileName, string targetFileName, string mapFileName, IContentType contentType)
        {
            var map = new CssSourceMap();

            map.Initialize(sourceFileName, targetFileName, mapFileName, contentType);

            if (map.IsDirty)
                return;

            AddToDictionary(map, sourceFileName, targetFileName);
        }

        private async static void AddToDictionary(CssSourceMap map, string sourceFileName, string targetFileName)
        {
            using (await _rwLock.WriteLockAsync())
            {
                _sourceMaps[targetFileName] = map.MapNodes;
            }
        }

        private void Initialize(string sourceFileName, string targetFileName, string mapFileName, IContentType contentType)
        {
            _parser = CssParserLocator.FindComponent(contentType).CreateParser();
            _directory = Path.GetDirectoryName(sourceFileName);
            IsDirty = !PopulateMap(targetFileName, mapFileName); // Begin two-steps initialization.
        }

        private bool PopulateMap(string targetFileName, string mapFileName)
        {
            var map = new SourceMapDefinition();

            try
            {
                map = Json.Decode<SourceMapDefinition>(File.ReadAllText(mapFileName));
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