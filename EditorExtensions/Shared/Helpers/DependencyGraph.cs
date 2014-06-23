using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Commands;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Helpers
{
    ///<summary>Maintains a graph of dependencies among files in the solution.</summary>
    /// <remarks>This class is completely decoupled from all Visual Studio concepts.</remarks>
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
        readonly AsyncReaderWriterLock rwLock = new AsyncReaderWriterLock();

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

            public override string ToString()
            {
                return FileName + " (" + dependencies.Count + " dependencies; " + dependents.Count + " dependents)";
            }
        }
        #endregion

        #region Graph Creation
        ///<summary>Gets the full paths to all files that the given file depends on.  (the dependencies need not exist).</summary>
        ///<remarks>This method will be called concurrently on arbitrary threads.</remarks>
        protected abstract Task<IEnumerable<string>> GetDependencyPaths(string fileName);

        ///<summary>Rescans the entire graph from a collection of source files, replacing the entire graph.</summary>
        ///<remarks>Although this method is async, it performs lots of synchronous work, and should not be called on a UI thread.</remarks>
        public async Task RescanAllAsync(IEnumerable<string> sourceFiles)
        {
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
                    foreach (var dependency in await item.dependencies)
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
            bool unused;
            return GetNode(filename, out unused);
        }

        GraphNode GetNode(string filename, out bool created)
        {
            filename = Path.GetFullPath(filename);
            GraphNode node;
            created = nodes.TryGetValue(filename, out node);
            if (!created)
                nodes.Add(filename, node = new GraphNode(filename));
            return node;
        }

        ///<summary>Reparses dependencies for a single file and updates the graph.</summary>
        ///<remarks>Although this method is async, it performs synchronous work, and should not be called on a UI thread.</remarks>
        public Task RescanFileAsync(string fileName)
        {
            var visited = new HashSet<string>();

            return RescanFileAsync(fileName, false, visited);
        }

        private async Task RescanFileAsync(string fileName, bool hasLock, HashSet<string> visited)
        {
            fileName = Path.GetFullPath(fileName);

            if (visited.Contains(fileName))
                return;

            visited.Add(fileName);

            var dependencies = GetDependencyPaths(fileName);

            using (hasLock ? new AsyncReaderWriterLock.Releaser() : await rwLock.WriteLockAsync())
            {

                bool created;
                var parentNode = GetNode(fileName);

                parentNode.ClearDependencies();

                foreach (var dependency in await dependencies)
                {
                    var childNode = GetNode(dependency, out created);
                    parentNode.AddDependency(childNode);

                    if (created)    // This will (and must) run synchronously, since it doesn't acquire the lock
                        await RescanFileAsync(childNode.FileName, true, visited);
                }
            }
        }
        #endregion
    }

    ///<summary>A DependencyGraph that reads Visual Studio solutions.</summary>
    ///<remarks>
    /// Derived classes must contain the following attributes:
    /// <code>
    ///     [Export(typeof(DependencyGraph))]
    ///     [Export(typeof(IFileSaveListener))]
    ///     [ContentType("YourTypeName")]
    /// </code>
    /// They must pass the same <see cref="IContentType"/> to the base constructor.
    ///</remarks>
    public abstract class VsDependencyGraph : DependencyGraph, IDisposable, IFileSaveListener
    {
        readonly ISet<string> extensions;
        ///<summary>Gets the ContentType of the files analyzed by this instance.</summary>
        public IContentType ContentType { get; private set; }

        private readonly DocumentEvents documentEvents = WebEssentialsPackage.DTE.Events.DocumentEvents;
        private readonly SolutionEvents solutionEvents = WebEssentialsPackage.DTE.Events.SolutionEvents;
        private readonly ProjectItemsEvents projectItemEvents = ((Events2)WebEssentialsPackage.DTE.Events).ProjectItemsEvents;

        protected VsDependencyGraph(IContentType contentType, IFileExtensionRegistryService fileExtensionRegistry)
        {
            ContentType = contentType;
            extensions = fileExtensionRegistry.GetFileExtensionSet(contentType);
            RescanComplete = TplExtensions.CompletedTask;
        }

        ///<summary>Gets a task that notifies callers when all rescanning is finished.  This property will always be assigned synchronously.</summary>
        /// <remarks>This is primarily meant for unit tests, to inspect the graph after async event handlers are finished.</remarks>
        public Task RescanComplete { get; private set; }


        ///<summary>Rescans all the entire graph from the source files in the current Visual Studio solution.</summary>
        ///<remarks>Although this method is async, it performs lots of synchronous work, and should not be called on a UI thread.</remarks>
        public Task RescanSolutionAsync()
        {
            var sourceFiles = ProjectHelpers.GetAllProjects()
                .Select(ProjectHelpers.GetRootFolder)
                .Where(p => !string.IsNullOrEmpty(p))
                .SelectMany(p => Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                .Where(f => extensions.Contains(Path.GetExtension(f)));
            return RescanAllAsync(sourceFiles);
        }

        bool isEnabled;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value) return;
                isEnabled = value;
                if (value)
                {
                    AddEventHandlers();
                    if (WebEssentialsPackage.DTE.Solution.IsOpen)
                        RescanComplete = Task.Run(() => RescanSolutionAsync()).HandleErrors("scanning solution for " + ContentType + " dependencies");
                }
                else
                    RemoveEventHandlers();
            }
        }

        private void AddEventHandlers()
        {
            solutionEvents.Opened += SolutionEvents_Opened;
            solutionEvents.ProjectAdded += SolutionEvents_ProjectAdded;
            projectItemEvents.ItemAdded += ProjectItemEvents_ItemAdded;
            documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
            projectItemEvents.ItemRenamed += ProjectItemEvents_ItemRenamed;
        }

        private void RemoveEventHandlers()
        {
            solutionEvents.Opened -= SolutionEvents_Opened;
            solutionEvents.ProjectAdded -= SolutionEvents_ProjectAdded;
            projectItemEvents.ItemAdded -= ProjectItemEvents_ItemAdded;
            documentEvents.DocumentSaved -= DocumentEvents_DocumentSaved;
            projectItemEvents.ItemRenamed -= ProjectItemEvents_ItemRenamed;
        }


        ///<summary>Releases all resources used by the VsDependencyGraph.</summary>
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        ///<summary>Releases the unmanaged resources used by the VsDependencyGraph and optionally releases the managed resources.</summary>
        ///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsEnabled = false;  // Remove event handlers
            }
        }

        #region Event Handlers
        private void ProjectItemEvents_ItemRenamed(ProjectItem projectItem, string oldName)
        {
            var fileName = projectItem.FileNames[1];
            RenameNestedItems(projectItem, fileName);

            if (extensions.Contains(Path.GetExtension(fileName)))
                RescanComplete = Task.Run(() => RescanFileAsync(fileName)).HandleErrors("parsing " + projectItem.Name + " for dependencies");
        }

        private static void RenameNestedItems(ProjectItem projectItem, string fileName)
        {
            var path = Path.GetDirectoryName(fileName);

            foreach (var item in projectItem.ProjectItems.Cast<ProjectItem>())
            {
                var trueExtension = string.Join("", item.Name.SkipWhile(x => x != '.'));
                var newFileName = string.Format(CultureInfo.CurrentCulture, "{0}{1}", FileHelpers.GetFileNameWithoutExtension(fileName), trueExtension);

                if (File.Exists(Path.Combine(path, newFileName)))
                    continue;

                item.Name = newFileName;
            }
        }

        private void DocumentEvents_DocumentSaved(Document document)
        {
            var fileName = document.Path;
            if (extensions.Contains(Path.GetExtension(fileName)))
                RescanComplete = Task.Run(() => RescanFileAsync(fileName)).HandleErrors("parsing " + document.Name + " for dependencies");
        }

        private void ProjectItemEvents_ItemAdded(ProjectItem projectItem)
        {
            var fileName = projectItem.FileNames[1];
            if (extensions.Contains(Path.GetExtension(fileName)))
                RescanComplete = Task.Run(() => RescanFileAsync(fileName)).HandleErrors("parsing " + projectItem.Name + " for dependencies");
        }

        private void SolutionEvents_ProjectAdded(Project project)
        {
            RescanComplete = Task.Run(() => RescanSolutionAsync()).HandleErrors("scanning solution for " + ContentType + " dependencies");
        }

        private void SolutionEvents_Opened()
        {
            RescanComplete = Task.Run(() => RescanSolutionAsync()).HandleErrors("scanning new solution for " + ContentType + " dependencies");
        }

        // This is triggered by the derived class' ContentType-specific [Export(typeof(IFileSaveListener))]
        public Task FileSaved(IContentType contentType, string path, bool forceSave, bool minifyInPlace)
        {
            if (IsEnabled)
                RescanComplete = Task.Run(() => RescanFileAsync(path)).HandleErrors("parsing " + path + " for dependencies");

            return null;
        }
        #endregion
    }

    public class CssDependencyGraph : VsDependencyGraph
    {
        public string Extension { get; private set; }

        readonly ICssParserFactory parserFactory;
        public CssDependencyGraph(string extension, IFileExtensionRegistryService fileExtensionRegistry)
            : base(fileExtensionRegistry.GetContentTypeForExtension(extension.TrimStart('.')), fileExtensionRegistry)
        {
            Extension = extension;
            parserFactory = CssParserLocator.FindComponent(ContentType);
        }

        protected async override Task<IEnumerable<string>> GetDependencyPaths(string fileName)
        {
            var sourceUri = new Uri(fileName);
            var cachedFileContent = await FileHelpers.ReadAllTextRetry(fileName);

            return new CssItemAggregator<string> { (ImportDirective id) => GetImportPaths(sourceUri, id) }
                            .Crawl(parserFactory.CreateParser().Parse(cachedFileContent, false));
        }

        private static IEnumerable<string> GetImportPaths(Uri sourceUri, ImportDirective importDirective)
        {
            try
            {
                return CssDocumentHelpers.GetSourceUrisFromImport(sourceUri, importDirective)
                                         .Where(t => t.Item1.IsFile && !t.Item1.OriginalString.StartsWith("//", StringComparison.Ordinal))    // Skip protocol-relative paths
                                         .Select(t => t.Item1.LocalPath);
            }
            catch (ArgumentException)
            {
                return Enumerable.Empty<string>();
            }
        }
    }

    [Export(typeof(DependencyGraph))]
    [Export(typeof(IFileSaveListener))]
    [ContentType("LESS")]
    public class LessDependencyGraph : CssDependencyGraph
    {
        [ImportingConstructor]
        public LessDependencyGraph(IFileExtensionRegistryService fileExtensionRegistry)
            : base(".less", fileExtensionRegistry)
        { }
    }

    [Export(typeof(DependencyGraph))]
    [Export(typeof(IFileSaveListener))]
    [ContentType("SCSS")]
    public class ScssDependencyGraph : CssDependencyGraph
    {
        [ImportingConstructor]
        public ScssDependencyGraph(IFileExtensionRegistryService fileExtensionRegistry)
            : base(".scss", fileExtensionRegistry)
        { }
    }
}