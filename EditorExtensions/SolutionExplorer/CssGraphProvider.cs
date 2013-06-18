using Microsoft.CSS.Core;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Microsoft.Web.Editor.CssContentTypeDefinition.CssContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [GraphProvider(Name = "CssGraphProvider")]
    public class CssGraphProvider : GraphProviderBase, IWpfTextViewCreationListener
    {
        private static Dictionary<Uri, IGraphContext> _cache = new Dictionary<Uri, IGraphContext>();

        public CssGraphProvider()
            : base()
        { }

        public void TextViewCreated(IWpfTextView textView)
        {
            ITextDocument document;
            textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document);

            if (document != null)
            {
                document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        private void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                Uri url = new Uri(e.FilePath, UriKind.Absolute);
                if (_cache.ContainsKey(url))
                {
                    BeginGetGraphData(_cache[url]);
                }
            }
        }

        public override void BeginGetGraphData(IGraphContext context)
        {
            RegisterImages();            

            if (context.Direction == GraphContextDirection.Contains)
            {
                CreateOutline(context);

                if (context.TrackChanges)
                {
                    ThreadPool.QueueUserWorkItem(CreateChildren, context);
                }
                else
                {
                    CreateChildren(context);
                }

                return; // Don't call OnCompleted
            }
            else if (context.Direction == GraphContextDirection.Self && context.RequestedProperties.Contains(DgmlNodeProperties.ContainsChildren))
            {
                foreach (GraphNode node in GetAllowedFiles(context.InputNodes))
                {
                    node.SetValue(DgmlNodeProperties.ContainsChildren, true);
                }

                foreach (var node in context.InputNodes.Where(n =>
                    n.HasCategory(CssGraphSchema.CssAtDirectivesParent) ||
                    n.HasCategory(CssGraphSchema.CssIdSelectorParent) ||
                    n.HasCategory(CssGraphSchema.CssClassSelectorParent)
                    ))
                {
                    node.SetValue(DgmlNodeProperties.ContainsChildren, true);
                }
            }

            // Signals that all results have been created.
            context.OnCompleted();
        }

        private void CreateOutline(object state)
        {
            IGraphContext context = (IGraphContext)state;

            using (var scope = new GraphTransactionScope())
            {
                //context.Graph.Links.Clear();
                //context.OutputNodes.Clear();

                context.Graph.Links.Remove(context.Graph.Links.Where(l => l.Label == "dir" || l.Label == "id" || l.Label == "class"));

                foreach (GraphNode node in GetAllowedFiles(context.InputNodes))
                {
                    Uri url = node.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File);
                    _cache[url] = context;

                    node.AddCategory(CssGraphSchema.CssFile);
                    CreateValues(context, node);
                }

                scope.Complete();
            }
        }

        private void CreateChildren(object state)
        {
            IGraphContext context = (IGraphContext)state;

            using (GraphTransactionScope scope = new GraphTransactionScope())
            {
                foreach (var node in context.InputNodes.Where(n => n.HasCategory(CssGraphSchema.CssAtDirectivesParent)))
                {
                    CreateChild<AtDirective>(context, node, CssGraphSchema.CssAtDirectives);
                    context.ReportProgress(1, 3, null);
                }

                foreach (var node in context.InputNodes.Where(n => n.HasCategory(CssGraphSchema.CssIdSelectorParent)))
                {
                    CreateChild<IdSelector>(context, node, CssGraphSchema.CssIdSelector);
                    context.ReportProgress(2, 3, null);
                }

                foreach (var node in context.InputNodes.Where(n => n.HasCategory(CssGraphSchema.CssClassSelectorParent)))
                {
                    CreateChild<ClassSelector>(context, node, CssGraphSchema.CssClassSelector);
                    context.ReportProgress(3, 3, null);
                }

                scope.Complete();
            }

            context.OnCompleted();
        }

        private void CreateValues(IGraphContext context, GraphNode node)
        {
            StyleSheet stylesheet = GetOrCreateStyleSheet(node, _fileService);
            GraphNodeId urlPart = node.Id + GraphNodeId.GetPartial(CssGraphSchema.CssStyleSheet, stylesheet);

            // @-Directives
            if (FindCssItems<AtDirective>(stylesheet).Count > 0)
            {
                GraphNode dirNode = CreateParentNode(context, urlPart, "@-Directives", CssGraphSchema.CssAtDirectivesParent);
                context.Graph.Links.GetOrCreate(node, dirNode, "dir", GraphCommonSchema.Contains);
                context.OutputNodes.Add(node);
            }

            // IDs
            if (FindCssItems<IdSelector>(stylesheet).Count > 0)
            {
                GraphNode idNode = CreateParentNode(context, urlPart, "IDs", CssGraphSchema.CssIdSelectorParent);
                context.Graph.Links.GetOrCreate(node, idNode, "id", GraphCommonSchema.Contains);
                context.OutputNodes.Add(idNode);
            }

            // Class
            if (FindCssItems<ClassSelector>(stylesheet).Count > 0)
            {
                GraphNode classNode = CreateParentNode(context, urlPart, "Classes", CssGraphSchema.CssClassSelectorParent);
                context.Graph.Links.GetOrCreate(node, classNode, "class", GraphCommonSchema.Contains);
                context.OutputNodes.Add(classNode);
            }
        }

        protected override string GetLabel(ParseItem item)
        {
            AtDirective dir = item as AtDirective;
            if (dir != null)
            {
                return "@" + dir.Keyword.Text;
            }

            return item.Text;
        }

        protected override IEnumerable<string> FileExtensions
        {
            get { return new[] { ".css", ".less" }; }
        }

        public override IEnumerable<GraphCommand> GetCommands(IEnumerable<GraphNode> nodes)
        {
            return new[] {
                new GraphCommand(GraphCommandDefinition.Contains, new[] { CssGraphSchema.CssAtDirectives }),
                new GraphCommand(GraphCommandDefinition.Contains, new[] { CssGraphSchema.CssIdSelector }),
                new GraphCommand(GraphCommandDefinition.Contains, new[] { CssGraphSchema.CssAtDirectivesParent }, trackChanges: true),
                new GraphCommand(GraphCommandDefinition.Contains, new[] { CssGraphSchema.CssIdSelectorParent }, trackChanges: true),
                new GraphCommand(GraphCommandDefinition.Contains, new[] { CssGraphSchema.CssClassSelector }),
                new GraphCommand(GraphCommandDefinition.Contains, new[] { CssGraphSchema.CssClassSelectorParent }, trackChanges: true),
            };
        }
    }
}
