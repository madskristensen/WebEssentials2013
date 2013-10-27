using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.ContainedLanguage;
using System.Reflection;
using Microsoft.Html.Core;
using Microsoft.Web.Core;
using Microsoft.Web.Editor;
using Microsoft.Html.Editor.Projection;
using Microsoft.Web.Editor.Composition;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    [Export(typeof(IContentTypeHandlerProvider))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownContentTypeHandlerProvider : IContentTypeHandlerProvider
    {
        [Import]
        public IContentTypeRegistryService ContentTypeRegistry { get; set; }

        public IContentTypeHandler GetContentTypeHandler() { return new MarkdownContentTypeHandler(ContentTypeRegistry); }
    }

    public class MarkdownContentTypeHandler : HtmlContentTypeHandler
    {
        static readonly Func<HtmlContentTypeHandler, List<LanguageBlockHandler>> GetLanguageBlockHandlerList =
            (Func<HtmlContentTypeHandler, List<LanguageBlockHandler>>)
            Delegate.CreateDelegate(
                typeof(Func<HtmlContentTypeHandler, List<LanguageBlockHandler>>),
                typeof(HtmlContentTypeHandler).GetProperty("LanguageBlockHandlers", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod
            );

        readonly IContentTypeRegistryService contentTypeRegistry;
        public MarkdownContentTypeHandler(IContentTypeRegistryService contentTypeRegistry)
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }

        public override ArtifactCollection CreateArtifactCollection() { return new MarkdownCodeArtifactCollection(new MarkdownCodeArtifactProcessor()); }

        protected override void CreateBlockHandlers()
        {
            base.CreateBlockHandlers();
            GetLanguageBlockHandlerList(this).Add(new CodeBlockBlockHandler(EditorTree, contentTypeRegistry));
        }

        public override void UpdateContainedLanguageBuffers()
        {
            // TODO: Call RemoveSpans() on each created LanguageProjectionBuffer iff IsRegenerationNeeded()
            base.UpdateContainedLanguageBuffers();
        }
    }

    class CodeBlockBlockHandler : ArtifactBasedBlockHandler
    {
        readonly IContentTypeRegistryService contentTypeRegistry;
        public CodeBlockBlockHandler(HtmlEditorTree tree, IContentTypeRegistryService contentTypeRegistry)
            : base(tree, contentTypeRegistry.GetContentType("code"))
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }
        protected override BufferGenerator CreateBufferGenerator()
        {
            return new MarkdownBufferGenerator(EditorTree, LanguageBlocks, contentTypeRegistry);
        }
        public override IContentType GetContentTypeOfLocation(int position)
        {
            LanguageBlock block = this.GetLanguageBlockOfLocation(position);
            if (block == null) return null;
            var alb = block as ArtifactLanguageBlock;
            if (alb == null)
                return contentTypeRegistry.GetContentType("text");

            return alb.ContentType ?? contentTypeRegistry.GetContentType("code");
        }

        protected override void BuildLanguageBlockCollection()
        {
            LanguageBlocks.Clear();
            foreach (MarkdownCodeArtifact current in EditorTree.RootNode.Tree.ArtifactCollection)
            {
                if (current.TreatAs == ArtifactTreatAs.Code)
                    LanguageBlocks.AddBlock(new ArtifactLanguageBlock(current, contentTypeRegistry.FromFriendlyName(current.Language)));
            }
            LanguageBlocks.SortByPosition();
        }
    }

    class ArtifactLanguageBlock : LanguageBlock
    {
        public ArtifactLanguageBlock(MarkdownCodeArtifact a, IContentType contentType)
            : base(a)
        {
            ContentType = contentType;
        }
        public IContentType ContentType { get; private set; }
    }
}
