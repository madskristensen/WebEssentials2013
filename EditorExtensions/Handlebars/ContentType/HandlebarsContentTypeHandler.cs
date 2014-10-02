using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.ContainedLanguage;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor.Formatting;

namespace MadsKristensen.EditorExtensions.Handlebars
{
    [Export(typeof(IContentTypeHandlerProvider))]
    [ContentType(HandlebarsContentTypeDefinition.HandlebarsContentType)]
    public class HandlebarsContentTypeHandlerProvider : IContentTypeHandlerProvider
    {
        [Import]
        public IContentTypeRegistryService ContentTypeRegistry { get; set; }

        public IContentTypeHandler GetContentTypeHandler() { return new HandlebarsContentTypeHandler(ContentTypeRegistry); }
    }

    public class HandlebarsContentTypeHandler : HtmlContentTypeHandler
    {
        static readonly Func<HtmlContentTypeHandler, List<LanguageBlockHandler>> GetLanguageBlockHandlerList =
            (Func<HtmlContentTypeHandler, List<LanguageBlockHandler>>)
            Delegate.CreateDelegate(
                typeof(Func<HtmlContentTypeHandler, List<LanguageBlockHandler>>),
                typeof(HtmlContentTypeHandler).GetProperty("LanguageBlockHandlers", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod
            );

        readonly IContentTypeRegistryService contentTypeRegistry;
        public HandlebarsContentTypeHandler(IContentTypeRegistryService contentTypeRegistry)
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }

        //public override ArtifactCollection CreateArtifactCollection() { return new HandlebarsCodeArtifactCollection(new HandlebarsCodeArtifactProcessor()); }

        //protected override void CreateBlockHandlers()
        //{
        //    base.CreateBlockHandlers();
        //    GetLanguageBlockHandlerList(this).Add(new CodeBlockBlockHandler(EditorTree, contentTypeRegistry));
        //}

        public override void UpdateContainedLanguageBuffers()
        {
            // TODO: Call RemoveSpans() on each created LanguageProjectionBuffer iff IsRegenerationNeeded()
            base.UpdateContainedLanguageBuffers();
        }
    }

    //class CodeBlockBlockHandler : ArtifactBasedBlockHandler
    //{
    //    readonly IContentTypeRegistryService contentTypeRegistry;
    //    public CodeBlockBlockHandler(HtmlEditorTree tree, IContentTypeRegistryService contentTypeRegistry)
    //        : base(tree, contentTypeRegistry.GetContentType("code"))
    //    {
    //        this.contentTypeRegistry = contentTypeRegistry;
    //    }
    //    protected override BufferGenerator CreateBufferGenerator()
    //    {
    //        return new HandlebarsBufferGenerator(EditorTree, LanguageBlocks, contentTypeRegistry);
    //    }
    //    public override IContentType GetContentTypeOfLocation(int position)
    //    {
    //        LanguageBlock block = this.GetLanguageBlockOfLocation(position);
    //        if (block == null) return null;
    //        var alb = block as ArtifactLanguageBlock;
    //        if (alb == null)
    //            return contentTypeRegistry.GetContentType("text");

    //        return alb.ContentType ?? contentTypeRegistry.GetContentType("code");
    //    }

    //    protected override void BuildLanguageBlockCollection()
    //    {
    //        LanguageBlocks.Clear();
    //        foreach (var artifact in EditorTree.RootNode.Tree.ArtifactCollection.OfType<CodeLineArtifact>())
    //        {
    //            var contentType = contentTypeRegistry.FromFriendlyName(artifact.BlockInfo.Language).ToEmbeddableContentType();
    //            if (contentType != null)
    //                LanguageBlocks.AddBlock(new ArtifactLanguageBlock(artifact, contentType));
    //        }
    //        LanguageBlocks.SortByPosition();
    //    }
    //}

    //class ArtifactLanguageBlock : LanguageBlock
    //{
    //    public ArtifactLanguageBlock(CodeLineArtifact a, IContentType contentType)
    //        : base(a)
    //    {
    //        ContentType = contentType;
    //    }
    //    public IContentType ContentType { get; private set; }
    //}

    // The HTML formatter doesn't work properly with Artifacts
    // unless you implement a whole bunch of internal features
    // for Razor (providing ArtifactGroups).  Plus, it doesn't
    // work properly with Artifacts in other ways (it swallows
    // separators).  I disable it entirely to avoid trouble.
    //[Export(typeof(IEditorFormatterProvider))]
    //[ContentType(HandlebarsContentTypeDefinition.HandlebarsContentType)]
    //public class MarkdownNonFormatterProvider : IEditorFormatterProvider
    //{
    //    public IEditorFormatter CreateFormatter() { return null; }
    //    public IEditorRangeFormatter CreateRangeFormatter() { return null; }
    //}
}