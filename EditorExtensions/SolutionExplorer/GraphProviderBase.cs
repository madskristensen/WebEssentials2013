using Microsoft.CSS.Core;
using System.Linq;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    public abstract class GraphProviderBase : IGraphProvider
    {
        private IServiceProvider _serviceProvider;
        private Dispatcher _dispatcher;
        private bool _imageRegistered;        

        public class Images
        {
            public const string Parent = "CssParent";
            public const string Child = "CssChild";
        }

        public GraphProviderBase()
        {
            this._dispatcher = Dispatcher.CurrentDispatcher;
            this._serviceProvider = EditorExtensionsPackage.Instance;
        }

        [Import]
        protected IFileExtensionRegistryService _fileService { get; set; }

        public abstract void BeginGetGraphData(IGraphContext context);

        protected abstract string GetLabel(ParseItem item);

        public abstract IEnumerable<GraphCommand> GetCommands(IEnumerable<GraphNode> nodes);

        public static StyleSheet GetOrCreateStyleSheet(GraphNode node, IFileExtensionRegistryService fileService)
        {
            StyleSheet stylesheet = node.Id.GetNestedValueByName<StyleSheet>(CssGraphSchema.CssStyleSheet);

            if (stylesheet == null)
            {
                Uri url = node.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File);
                IContentType contentType = fileService.GetContentTypeForExtension(Path.GetExtension(url.LocalPath));
                string contentTypeName = (contentType != null) ? contentType.TypeName : CssContentTypeDefinition.CssContentType;

                ICssParser parser = contentTypeName == LessContentTypeDefinition.LessContentType ? new LessParser() : new CssParser();
                stylesheet = parser.Parse(File.ReadAllText(url.LocalPath), false);
            }

            return stylesheet;
        }

        protected GraphNode CreateParentNode(IGraphContext context, GraphNodeId urlPart, string label, GraphCategory category)
        {
            GraphNodeId idPart = urlPart + GraphNodeId.GetPartial(CssGraphSchema.CssType, label);
            GraphNode idNode = context.Graph.Nodes.GetOrCreate(idPart, label, category);
            idNode[DgmlNodeProperties.Icon] = Images.Parent;

            return idNode;
        }

        protected void CreateChild<T>(IGraphContext context, GraphNode node, GraphCategory category) where T : ParseItem
        {
            StyleSheet stylesheet = node.Id.GetNestedValueByName<StyleSheet>(CssGraphSchema.CssStyleSheet);

            IList<T> items = FindCssItems<T>(stylesheet);
            List<string> cache = new List<string>();

            foreach (ParseItem item in items)
            {
                string label = GetLabel(item);
                if (!cache.Contains(label))
                {
                    CreateNode(context, node, item, category);
                    cache.Add(label);
                }
            }
        }

        protected static IList<T> FindCssItems<T>(StyleSheet stylesheet) where T : ParseItem
        {
            var visitor = new CssItemCollector<T>(true);
            stylesheet.Accept(visitor);
            return visitor.Items;
        }

        protected virtual void CreateNode(IGraphContext context, GraphNode file, ParseItem item, GraphCategory category)
        {
            GraphNodeId id = file.Id + GraphNodeId.GetPartial(CssGraphSchema.CssParseItem, item);
            GraphNode node = context.Graph.Nodes.GetOrCreate(id, GetLabel(item), category);
            node[DgmlNodeProperties.Icon] = Images.Child;

            context.Graph.Links.GetOrCreate(file, node, null, GraphCommonSchema.Contains);
            context.OutputNodes.Add(node);
        }

        protected abstract IEnumerable<string> FileExtensions { get; }

        protected virtual IEnumerable<GraphNode> GetAllowedFiles(IEnumerable<GraphNode> nodes)
        {
            foreach (GraphNode node in nodes.Where(n => n.HasCategory(CodeNodeCategories.ProjectItem)))
            {
                Uri file = node.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File);
                if (file != null && FileExtensions.Contains(Path.GetExtension(file.LocalPath)))// (file.LocalPath.EndsWith(".css") || file.LocalPath.EndsWith(".less")))
                {
                    yield return node;
                }
            }
        }

        public T GetExtension<T>(GraphObject graphObject, T previous) where T : class
        {
            if (typeof(T) == typeof(IGraphNavigateToItem))
            {
                return new GraphNodeNavigator() as T;
            }
            return null;
        }

        public Graph Schema
        {
            get { return null; }
        }

        protected void RegisterImages()
        {
            if (!_imageRegistered && _serviceProvider != null)
            {
                _dispatcher.Invoke(() =>
                {
                    _imageRegistered = true;

                    //ImageSource imgDir = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/graph_atdir.png", UriKind.RelativeOrAbsolute));
                    //ImageSource imgId = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/graph_id.png", UriKind.RelativeOrAbsolute));
                    //ImageSource imgClass = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/graph_class.png", UriKind.RelativeOrAbsolute));
                    ImageSource imgParent = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/graph_parent.png", UriKind.RelativeOrAbsolute));
                    ImageSource imgChild = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/graph_class.png", UriKind.RelativeOrAbsolute));

                    IVsImageService imageService = (IVsImageService)_serviceProvider.GetService(typeof(SVsImageService));
                    //imageService.Add(_imageDir, WpfPropertyValue.CreateIconObject(imgDir));
                    //imageService.Add(_imageId, WpfPropertyValue.CreateIconObject(imgId));
                    //imageService.Add(_imageClass, WpfPropertyValue.CreateIconObject(imgClass));
                    imageService.Add(Images.Parent, WpfPropertyValue.CreateIconObject(imgParent));
                    imageService.Add(Images.Child, WpfPropertyValue.CreateIconObject(imgChild));
                });
            }
        }
    }
}
