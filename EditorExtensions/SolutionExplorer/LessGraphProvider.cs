using Microsoft.CSS.Core;
using Microsoft.Less.Core;
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
    [ContentType("LESS")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [GraphProvider(Name = "LessGraphProvider")]
    public class LessGraphProvider : GraphProviderBase, IWpfTextViewCreationListener
    {
        private static Dictionary<Uri, IGraphContext> _cache = new Dictionary<Uri, IGraphContext>();

        public LessGraphProvider()
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

                return;
            }
            else if (context.Direction == GraphContextDirection.Self && context.RequestedProperties.Contains(DgmlNodeProperties.ContainsChildren))
            {
                foreach (GraphNode node in GetAllowedFiles(context.InputNodes))
                {
                    node.SetValue(DgmlNodeProperties.ContainsChildren, true);
                }

                foreach (var node in context.InputNodes.Where(n =>
                    n.HasCategory(LessGraphSchema.LessMixinParent) ||
                    n.HasCategory(LessGraphSchema.LessVariableParent)
                    ))
                {
                    node.SetValue(DgmlNodeProperties.ContainsChildren, true);
                }
            }

            // Signals that all results have been created.
            context.OnCompleted();
        }

        private void CreateOutline(IGraphContext context)
        {
            using (var scope = new GraphTransactionScope())
            {
                context.Graph.Links.Remove(context.Graph.Links.Where(l => l.Label == "mixin" || l.Label == "hat"));

                foreach (GraphNode node in GetAllowedFiles(context.InputNodes))
                {
                    Uri url = node.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File);
                    _cache[url] = context;

                    node.AddCategory(LessGraphSchema.LessFile);
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
                foreach (var node in context.InputNodes.Where(n => n.HasCategory(LessGraphSchema.LessMixinParent)))
                {
                    CreateChild<LessMixinDeclaration>(context, node, LessGraphSchema.LessMixin);
                    context.ReportProgress(1, 2, null);
                }

                foreach (var node in context.InputNodes.Where(n => n.HasCategory(LessGraphSchema.LessVariableParent)))
                {
                    CreateChild<LessVariableDeclaration>(context, node, LessGraphSchema.LessVariable);
                    context.ReportProgress(2, 2, null);
                }

                scope.Complete();
            }

            context.OnCompleted();
        }

        private void CreateValues(IGraphContext context, GraphNode node)
        {
            StyleSheet stylesheet = CssGraphProvider.GetOrCreateStyleSheet(node, _fileService);
            GraphNodeId urlPart = node.Id + GraphNodeId.GetPartial(CssGraphSchema.CssStyleSheet, stylesheet);

            // Mixin
            if (FindCssItems<LessMixinDeclaration>(stylesheet).Count > 0)
            {
                GraphNode mixinNode = CreateParentNode(context, urlPart, "Mixins", LessGraphSchema.LessMixinParent);
                context.Graph.Links.GetOrCreate(node, mixinNode, "mixin", GraphCommonSchema.Contains);
                context.OutputNodes.Add(mixinNode);
            }

            // Variable
            if (FindCssItems<LessVariableDeclaration>(stylesheet).Count > 0)
            {
                GraphNode variableNode = CreateParentNode(context, urlPart, "Variables", LessGraphSchema.LessVariableParent);
                context.Graph.Links.GetOrCreate(node, variableNode, "hat", GraphCommonSchema.Contains);
                context.OutputNodes.Add(variableNode);
            }
        }

        protected override string GetLabel(ParseItem item)
        {
            LessMixinDeclaration mixin = item as LessMixinDeclaration;
            if (mixin != null)
            {
                return mixin.MixinName.Text.TrimEnd('(');
            }

            LessVariableDeclaration variable = item as LessVariableDeclaration;
            if (variable != null)
            {
                return variable.VariableName.Text;
            }

            return item.Text;
        }

        protected override IEnumerable<string> FileExtensions
        {
            get { return new[] { ".less" }; }
        }

        public override IEnumerable<GraphCommand> GetCommands(IEnumerable<GraphNode> nodes)
        {
            return new[] {
                new GraphCommand(GraphCommandDefinition.Contains, new[] { LessGraphSchema.LessMixin}),
                new GraphCommand(GraphCommandDefinition.Contains, new[] { LessGraphSchema.LessMixinParent}, trackChanges: true),
                new GraphCommand(GraphCommandDefinition.Contains, new[] { LessGraphSchema.LessVariable}),
                new GraphCommand(GraphCommandDefinition.Contains, new[] { LessGraphSchema.LessVariableParent}, trackChanges: true),
            };
        }
    }
}
