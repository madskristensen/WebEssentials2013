using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Helpers
{
    ///<summary>Maintains a graph of dependencies among files in the solution.</summary>
    public abstract class DependencyGraph
    {
        // This dictionary and graph also contains nodes
        // for files that do not (yet) exist, as well as
        // files that have been deleted.  This allows us
        // to handle such files after they're recreated,
        // including untouched imports from other files.
        // In other words, you can never remove from the
        // graph, except by clearing & re-scanning.
        // This is synchronized by rwLock.
        readonly Dictionary<string, GraphNode> nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);
        readonly ISet<string> extensions;


        readonly AsyncReaderWriterLock rwLock = new AsyncReaderWriterLock();

        ///<summary>Gets the ContentType of the files analyzed by this instance.</summary>
        public IContentType ContentType { get; private set; }

        protected DependencyGraph(IContentType contentType, IFileExtensionRegistryService fileExtensionRegistry)
        {
            ContentType = contentType;
            extensions = fileExtensionRegistry.GetFileExtensionSet(contentType);
        }

        #region Graph Consumption
        ///<summary>Gets all files that directly depend on the specified file.</summary>
        public async Task<IEnumerable<string>> GetDirectDependentsAsync(string fileName)
        {
            fileName = Path.GetFullPath(fileName);

            using (await rwLock.ReadLockAsync())
            {
                GraphNode fileNode;
                if (!nodes.TryGetValue(fileName, out fileNode))
                    return Enumerable.Empty<string>();
                return fileNode.Dependents.Select(d => d.FileName);
            }
        }
        ///<summary>Gets all files that indirectly depend on the specified file.</summary>
        public async Task<IEnumerable<string>> GetRecursiveDependentsAsync(string fileName)
        {
            HashSet<GraphNode> visited;
            fileName = Path.GetFullPath(fileName);
            using (await rwLock.ReadLockAsync())
            {
                GraphNode rootNode;
                if (!nodes.TryGetValue(fileName, out rootNode))
                    return Enumerable.Empty<string>();

                var stack = new Stack<GraphNode>();
                stack.Push(rootNode);
                visited = new HashSet<GraphNode> { rootNode };
                while (stack.Count > 0)
                {
                    foreach (var child in stack.Pop().Dependents)
                    {
                        if (!visited.Add(child)) continue;
                        stack.Push(child);
                    }
                }
                // Don't return the original file.
                visited.Remove(rootNode);
            }
            return visited.Select(n => n.FileName);
        }
        private class GraphNode
        {
            // Protected by parent graph's rwLock
            readonly HashSet<GraphNode> dependencies = new HashSet<GraphNode>();
            readonly HashSet<GraphNode> dependents = new HashSet<GraphNode>();

            public string FileName { get; private set; }

            // The LINQ Contains() extension method will call into the underlying HashSet<T>.

            ///<summary>Gets the nodes that this file depends on.</summary>
            public IEnumerable<GraphNode> Dependencies { get { return dependencies; } }
            ///<summary>Gets the nodes that depend on this file.</summary>
            public IEnumerable<GraphNode> Dependents { get { return dependents; } }

            public GraphNode(string fileName)
            {
                FileName = fileName;
            }

            ///<summary>Marks this node as depending on the specified node, adding an edge to the graph if one does not exist already.</summary>
            ///<returns>True if an edge was added; false if the dependency already existed.</returns>
            public bool AddDependency(GraphNode node)
            {
                if (!dependencies.Add(node))
                    return false;
                node.dependents.Add(this);
                return true;
            }

            ///<summary>Removes all edges for nodes that this file depends on.  Call this method, inside a write lock, before reparsing the file.</summary>
            public void ClearDependencies()
            {
                foreach (var child in Dependencies)
                    child.dependents.Remove(this);
                dependencies.Clear();
            }
        }
        #endregion

        #region Graph Creation
        ///<summary>Gets the full paths to all files that the given file depends on.  (the dependencies need not exist).</summary>
        protected abstract IEnumerable<string> GetDependencyPaths(string fileName);
        ///<summary>Rescans all nodes from the filesystem.</summary>
        ///<remarks>Although this method is async, it performs lots of synchronous work, and should not be called on a UI thread.</remarks>
        public async Task Rescan()
        {
            var sourceFiles = ProjectHelpers.GetAllProjects()
                .Select(ProjectHelpers.GetRootFolder)
                .Where(p => !string.IsNullOrEmpty(p))
                .SelectMany(p => Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                .Where(f => extensions.Contains(Path.GetExtension(f)));

            // Parse all of the files in the background, then update the dictionary on one thread
            var dependencies = sourceFiles
                .AsParallel()
                .Select(f => new { FileName = f, dependencies = GetDependencyPaths(f) });
            using (await rwLock.WriteLockAsync())
            {
                nodes.Clear();
                foreach (var item in dependencies)
                {
                    var parentNode = GetNode(item.FileName);
                    foreach (var dependency in item.dependencies)
                        parentNode.AddDependency(GetNode(dependency));
                }
            }
#if false
            nodes.Clear();
            Parallel.ForEach(
                sourceFiles,
                () => ImmutableStack<Tuple<string, IEnumerable<string>>>.Empty,
                (filename, state, stack) => stack.Push(Tuple.Create(filename, GetDependencyPaths(filename))),
                stack =>
                {
                    foreach (var item in stack)
                    {
                        var parentNode = GetNode(item.Item1);
                        foreach (var dependency in item.Item2)
                            parentNode.AddDependency(GetNode(dependency));
                    }
                }
             );
#endif
        }
        GraphNode GetNode(string filename)
        {
            filename = Path.GetFullPath(filename);
            GraphNode node;
            if (!nodes.TryGetValue(filename, out node))
                nodes.Add(filename, node = new GraphNode(filename));
            return node;
        }
        #endregion

        #region Graph Updates

        #endregion
    }

    public class CssDependencyGraph<TParser> : DependencyGraph where TParser : CssParser, new()
    {
        public string Extension { get; private set; }
        public CssDependencyGraph(string extension, IFileExtensionRegistryService fileExtensionRegistry)
            : base(fileExtensionRegistry.GetContentTypeForExtension(extension.TrimStart('.')), fileExtensionRegistry)
        {
            Extension = extension;
        }

        protected override IEnumerable<string> GetDependencyPaths(string fileName)
        {
            return new CssItemAggregator<string> { (ImportDirective i) => i.FileName == null ? i.Url.UrlString.Text : i.FileName.Text }
                            .Crawl(new TParser().Parse(File.ReadAllText(fileName), false))
                            .Select(f => Path.Combine(Path.GetDirectoryName(fileName), f.Trim('"', '\'')))
                            .Select(f => f.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) ? f : f + Extension);
        }
    }
    [Export]
    public class LessDependencyGraph : CssDependencyGraph<LessParser>
    {
        [ImportingConstructor]
        public LessDependencyGraph(IFileExtensionRegistryService fileExtensionRegistry) : base(".less", fileExtensionRegistry)
        {
        }
    }
}