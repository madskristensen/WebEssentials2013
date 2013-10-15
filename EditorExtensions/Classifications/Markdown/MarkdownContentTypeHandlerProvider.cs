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

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    [Export(typeof(IContentTypeHandlerProvider))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownContentTypeHandlerProvider : IContentTypeHandlerProvider
    {
        [Import]
        public IContentTypeRegistryService ContentTypeRegistry { get; set; }

        public IContentTypeHandler GetContentTypeHandler()
        {
            return new MarkdownContentTypeHandler(ContentTypeRegistry);
        }
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

        protected override void CreateBlockHandlers()
        {
            base.CreateBlockHandlers();
            GetLanguageBlockHandlerList(this).Add(new CodeBlockBlockHandler(EditorTree, contentTypeRegistry));
        }

        public override void Init(HtmlEditorTree editorTree)
        {
            base.Init(editorTree);
            ContainedLanguageSettings.FormatOnPaste = false;
            ContainedLanguageSettings.EnableSyntaxCheck = false;
        }

        public override IContentType GetContentTypeOfLocation(int position)
        {
            int itemContaining = EditorTree.ArtifactCollection.GetItemContaining(position);
            if (itemContaining >= 0)
            {
                IArtifact artifact = EditorTree.ArtifactCollection[itemContaining];
                if (artifact.TreatAs == ArtifactTreatAs.Comment)
                {
                    return contentTypeRegistry.GetContentType("text");
                }
            }
            return base.GetContentTypeOfLocation(position);
        }
        public override ArtifactCollection CreateArtifactCollection()
        {
            return new ArtifactCollection(new MarkdownCodeArtifactProcessor());
        }
    }

    class CodeBlockBlockHandler : ArtifactBasedBlockHandler
    {
        readonly IContentTypeRegistryService contentTypeRegistry;
        public CodeBlockBlockHandler(HtmlEditorTree tree, IContentTypeRegistryService contentTypeRegistry)
            : base(tree, contentTypeRegistry.GetContentType("htmlx"))
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
            if (string.IsNullOrWhiteSpace(alb.Language))
                return contentTypeRegistry.GetContentType("code");

            return contentTypeRegistry.GetContentType(alb.Language);
        }

        protected override void BuildLanguageBlockCollection()
        {
            ArtifactCollection artifactCollection = base.EditorTree.RootNode.Tree.ArtifactCollection;
            base.LanguageBlocks.Clear();
            foreach (MarkdownCodeArtifact current in artifactCollection)
            {
                if (current.TreatAs == ArtifactTreatAs.Code)
                {
                    base.LanguageBlocks.AddBlock(new ArtifactLanguageBlock(current));
                }
            }
            base.LanguageBlocks.SortByPosition();
        }

    }

    class ArtifactLanguageBlock : LanguageBlock
    {
        public ArtifactLanguageBlock(MarkdownCodeArtifact a) : base(a) { Language = a.Language; }
        public string Language { get; private set; }
    }

    class MarkdownBufferGenerator : ArtifactBasedBufferGenerator
    {
        readonly IContentTypeRegistryService contentTypeRegistry;
        public MarkdownBufferGenerator(HtmlEditorTree editorTree, LanguageBlockCollection languageBlocks, IContentTypeRegistryService contentTypeRegistry)
            : base(editorTree, languageBlocks)
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }


        protected override void RegenerateBuffer()
        {
            if (!this.EnsureProjectionBuffer())
            {
                return;
            }
            base.RegenerateBuffer();

            foreach (var g in EditorTree.RootNode.Tree.ArtifactCollection.OfType<MarkdownCodeArtifact>()
                                        .GroupBy(a => a.Language))
            {
                var contentType = contentTypeRegistry.GetContentType(String.IsNullOrWhiteSpace(g.Key) ? "code" : g.Key);
                var pBuffer = ProjectionBufferManager.GetProjectionBuffer(contentType);

                StringBuilder fullSource = new StringBuilder();

                List<ProjectionMapping> list = new List<ProjectionMapping>();

                ITextSnapshot textSnapshot = base.EditorTree.TextSnapshot;

                this.AppendHeader(fullSource);
                foreach (var artifact in g)
                {

                    if (artifact.Start >= textSnapshot.Length || artifact.End > textSnapshot.Length || artifact.TreatAs != ArtifactTreatAs.Code)
                        continue;

                    fullSource.Append(this.BeginExternalSource);
                    int artifactStart = fullSource.Length;

                    ITextRange innerRange = artifact.InnerRange;
                    fullSource.Append(textSnapshot.GetText(innerRange.Start, innerRange.Length));
                    fullSource.Append(this.EndExternalSource);

                    ProjectionMapping item = new ProjectionMapping(innerRange.Start, artifactStart, innerRange.Length, AdditionalContentInclusion.All);
                    list.Add(item);
                }

                this.AppendFooter(fullSource);
                pBuffer.SetTextAndMappings(fullSource.ToString(), list.ToArray());
            }
        }

    }


    public class MarkdownCodeArtifactProcessor : IArtifactProcessor
    {
        public ArtifactCollection CreateArtifactCollection()
        {
            return new ArtifactCollection(this);
        }

        public void GetArtifacts(ITextProvider text, ArtifactCollection artifactCollection)
        {
            var parser = new MarkdownParser(new CharacterStream(text));
            parser.ArtifactFound += (s, e) => artifactCollection.Add(e.Artifact);
            parser.Parse();
        }

        public bool IsReady { get { return true; } }

        public string LeftSeparator { get { return "`"; } }
        public string RightSeparator { get { return "`"; } }
        public string LeftCommentSeparator { get { return "<!--"; } }
        public string RightCommentSeparator { get { return "<!--"; } }
    }
}
